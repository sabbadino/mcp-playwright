using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using playwright.test.generator.Abstractions;
using playwright.test.generator.IocConventions;
using playwright.test.generator.Plugins;
using playwright.test.generator.Settings;

namespace playwright.test.generator.Services;




public class PlayWrightTestGenerator : IPlayWrightTestGenerator, ISingletonScope
{
        private readonly PlayWrightTestGeneratorOptions _options;
    private readonly IEnumerable<KernelWrapper> _kernelWrappers;
    private readonly ITemplatesProvider _templatesProvider;

    public PlayWrightTestGenerator(IOptions<PlayWrightTestGeneratorOptions> options, IEnumerable<KernelWrapper> kernelWrappers, ITemplatesProvider templatesProvider)
    {
            _options = options.Value;
        _kernelWrappers = kernelWrappers;
        _templatesProvider = templatesProvider;
        // Initialization code here
    }

    private static ChatOptions CreateChatOptions(KernelWrapper kernelWrapper)
    {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        IEnumerable<AIFunction> aiFunctions = kernelWrapper.Kernel.Plugins.SelectMany(kp => kp.AsAIFunctions());
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return new ChatOptions { Temperature = kernelWrapper.KernelSettings.Temperature, ToolMode = ChatToolMode.Auto
            , Tools = [.. aiFunctions ]
        };    
    }

    private KernelWrapper GetKernelWrapper(string kernelName)
    {
        var kernelWrapper = _kernelWrappers.Single(k => k.KernelSettings.IsDefault);
        if (!string.IsNullOrWhiteSpace(kernelName))
        {
            kernelWrapper = _kernelWrappers.SingleOrDefault(k => string.Equals(k.KernelSettings.Name, kernelName, StringComparison.OrdinalIgnoreCase));
            if (kernelWrapper == null)
            {
                throw new SemanticKernelException($"Kernel with name {kernelName} not found.");
            }
        }
        return kernelWrapper;
    }

    // Add methods to generate Playwright tests based on the provided options and kernel settings
    public async Task<GenerateTestResult> GenerateTest(GenerateTestRequest generateTestRequest,CancellationToken cancellationToken = default)
    {
        var kernelWrapper = GetKernelWrapper(generateTestRequest.KernelName);
        var chatClient  = kernelWrapper.Kernel.GetRequiredService<IChatClient>();
        var history = new List<ChatMessage>();    
        history.Add(new ChatMessage (ChatRole.System,_templatesProvider.GetTemplate(kernelWrapper.KernelSettings.SystemMessageName).Replace(TestGeneratorsConstants.GenerateRetriesNumberPlaceholder,_options.ScriptFixRetries.ToString())));
        history.Add(new ChatMessage(ChatRole.User, generateTestRequest.ToUserMessage()));
        var chatOptions = CreateChatOptions(kernelWrapper);
        var response = await chatClient.GetResponseAsync(history,chatOptions, cancellationToken);

        /*
         * dbug: Microsoft.Extensions.AI.FunctionInvokingChatClient[807273242]
      Invoking 0_browser_install.
fail: Microsoft.Extensions.AI.FunctionInvokingChatClient[1784604714]
      0_browser_install invocation failed.
      System.InvalidOperationException: No service for type 'System.Collections.Generic.IEnumerable`1[Microsoft.SemanticKernel.KernelPlugin]' has been registered.
         at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService(IServiceProvider provider, Type serviceType)
         at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService[T](IServiceProvider provider)
         at Microsoft.SemanticKernel.Kernel..ctor(IServiceProvider services, KernelPluginCollection plugins)
         at Microsoft.SemanticKernel.KernelFunction.InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
         at Microsoft.Extensions.AI.KernelFunctionInvokingChatClient.InvokeFunctionAsync(FunctionInvocationContext context, CancellationToken cancellationToken)
         at Microsoft.Extensions.AI.FunctionInvokingChatClient.InstrumentedInvokeFunctionAsync(FunctionInvocationContext context, CancellationToken cancellationToken)
dbug: Microsoft.Extensions.AI.FunctionInvokingChatClient[1098781176]
      0_browser_install invocation completed. Duration: 00:00:00.0958642
info: Microsoft.Extensions.AI.OpenTelemetryChatClient[1]
      {}
info: Microsoft.Extensions.AI.OpenTelemetryChatClient[1]
      {}
info: Microsoft.Extensions.AI.OpenTelemetryChatClient[1]
      {"tool_calls":[{"id":"call_g18u4TQDAyrgKKWZqeDOgRws","type":"function","function":{"name":"0_browser_navigate"}}]}
info: Microsoft.Extensions.AI.OpenTelemetryChatClient[1]
      {"id":"call_g18u4TQDAyrgKKWZqeDOgRws"}
info: Microsoft.Extensions.AI.OpenTelemetryChatClient[1]
      {"tool_calls":[{"id":"call_x2MeHaM6sNWK3A6NuEdC3K7p","type":"function","function":{"name":"0_browser_install"}}]}
info: Microsoft.Extensions.AI.OpenTelemetryChatClient[1]
      {"id":"call_x2MeHaM6sNWK3A6NuEdC3K7p"}
         */

        ArgumentNullException.ThrowIfNull(response);
        if(response.Messages.Count==0)
        {
            throw new SemanticKernelException($"No response received from the chat client. (response.Messages.Count==0)");    
        }
        //var assistantMessages = history.Where(m => m.Role == AuthorRole.Assistant && m is OpenAIChatMessageContent).Cast<OpenAIChatMessageContent>();
        //var toolCallToPlaywrightTestScriptPlugin = assistantMessages.Where(m => m.ToolCalls.Select(t=> t.FunctionName).Contains($"{nameof(PlaywrightTestScriptPlugin)}-{PlaywrightTestScriptPlugin.KernelFunctionName}")).ToList();
        //var testScript = "";
        //bool hasToolCallToPlaywrightTestScriptPlugin = toolCallToPlaywrightTestScriptPlugin.Count > 0;  
        //if (toolCallToPlaywrightTestScriptPlugin.Count>0)
        //{
        //    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(toolCallToPlaywrightTestScriptPlugin.Last().ToolCalls.First()?.FunctionArguments);
        //    ArgumentNullException.ThrowIfNull(dict);
        //    testScript = dict.First().Value;
        //}
        //var errorContent= string.Empty;    
        //var testPass = response[response.Count - 1].Content?.StartsWith("TEST OK", StringComparison.OrdinalIgnoreCase) ?? false;
        //if(!testPass)
        //{
        //    var errorFiles = Directory.GetFiles("./test-results", "error-context.md", SearchOption.AllDirectories);
        //    if (errorFiles.Length > 0)
        //    {
        //        errorContent = await File.ReadAllTextAsync(errorFiles[0]);
        //    }   
        //}
        //return new GenerateTestResult
        //{
        //    Id = generateTestRequest.Id,    
        //    Text = response[response.Count - 1].Content??"",
        //    TestScript = testScript,
        //    ScriptAvailable =  hasToolCallToPlaywrightTestScriptPlugin,
        //    TestPass = testPass,
        //    ErrorContent = errorContent,    
        //};
        return null;
    }

}

internal static class GenerateTestRequestExtensions
{
    internal static string ToUserMessage(this GenerateTestRequest source) {
        ArgumentNullException.ThrowIfNull(source);
        if(source.Steps.Count == 0)
        {
            throw new ArgumentException("At least one step is required to generate a test.", nameof(source.Steps));
        }   
        var sb = new StringBuilder();
        sb.AppendLine("scenario  steps:"); 
        foreach (var i in source.Steps.Index())
        {
            sb.AppendLine($"{i.Index+1}: {i.Item.StepType.ToString()}: {i.Item.Text}"); 
        }
        return sb.ToString();
    }
}
