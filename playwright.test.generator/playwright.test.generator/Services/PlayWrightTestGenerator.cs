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
        IEnumerable<AIFunction> aiFunctions = kernelWrapper.Kernel.Plugins.SelectMany(kp => kp.AsAIFunctions(kernelWrapper.Kernel));
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
    public async Task<GenerateTestResult> GenerateTestIChatClient(GenerateTestRequest generateTestRequest,CancellationToken cancellationToken = default)
    {
        var kernelWrapper = GetKernelWrapper(generateTestRequest.KernelName);
        var chatClient  = kernelWrapper.Kernel.GetRequiredService<IChatClient>();
        var history = new List<ChatMessage>();    
        history.Add(new ChatMessage (ChatRole.System,_templatesProvider.GetTemplate(kernelWrapper.KernelSettings.SystemMessageName).Replace(TestGeneratorsConstants.GenerateRetriesNumberPlaceholder,_options.ScriptFixRetries.ToString())));
        history.Add(new ChatMessage(ChatRole.User, generateTestRequest.ToUserMessage()));
        var chatOptions = CreateChatOptions(kernelWrapper);
        var response = await chatClient.GetResponseAsync(history,chatOptions, cancellationToken);

     
     

        ArgumentNullException.ThrowIfNull(response);
        if(response.Messages.Count==0)
        {
            throw new SemanticKernelException($"No response received from the chat client. (response.Messages.Count==0)");    
        }
        var lastFunctionCallContext = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant && m.Contents.LastOrDefault() is Microsoft.Extensions.AI.FunctionCallContent cc)?.Contents.LastOrDefault(c => {
            if (c is Microsoft.Extensions.AI.FunctionCallContent x) {
                return x.Name.EndsWith(PlaywrightTestScriptPlugin.KernelFunctionName, StringComparison.OrdinalIgnoreCase);
            }
            return false; }) as Microsoft.Extensions.AI.FunctionCallContent;
        var testScript = "";
        bool hasToolCallToPlaywrightTestScriptPlugin = false;
        if (lastFunctionCallContext != null) {
            testScript = lastFunctionCallContext.Arguments?.First().Value as string ??"";
            hasToolCallToPlaywrightTestScriptPlugin = true;
        }
     
        var errorContent = string.Empty;
        var reply = response.Messages[response.Messages.Count - 1].Text;
        var testPass = reply.StartsWith("TEST OK", StringComparison.OrdinalIgnoreCase) ;
        if (!testPass)
        {
            var errorFiles = Directory.GetFiles("./test-results", "error-context.md", SearchOption.AllDirectories);
            if (errorFiles.Length > 0)
            {
                errorContent = await File.ReadAllTextAsync(errorFiles[0]);
}
        }
        return new GenerateTestResult
        {
            Id = generateTestRequest.Id,
            Text = reply,
            TestScript = testScript,
            ScriptAvailable = hasToolCallToPlaywrightTestScriptPlugin,
            TestPass = testPass,
            ErrorContent = errorContent,
        };
    }

    private static PromptExecutionSettings CreatePromptExecutionSettings(KernelWrapper kernelWrapper)
    {
        return kernelWrapper.KernelSettings.Model?.Category switch
        {
            ModelCategory.AzureOpenAi => new AzureOpenAIPromptExecutionSettings
            {
                Temperature = kernelWrapper.KernelSettings.Temperature
            },
            ModelCategory.OpenAi => new OpenAIPromptExecutionSettings
            {
                Temperature = kernelWrapper.KernelSettings.Temperature
            },
            //            ModelCategory.Gemini =>
            //#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            //                new GeminiPromptExecutionSettings
            //                {
            //                    Temperature = kernelWrapper.KernelSettings.Temperature
            //                },
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            _ => throw new SemanticKernelException($"Model category {kernelWrapper.KernelSettings.Model?.Category} is not supported"),
        };
    }
    public async Task<GenerateTestResult> GenerateTestIChatClientCompletion(GenerateTestRequest generateTestRequest, CancellationToken cancellationToken = default)
    {
        var kernelWrapper = GetKernelWrapper(generateTestRequest.KernelName);
        var chatClient = kernelWrapper.Kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddSystemMessage(_templatesProvider.GetTemplate(kernelWrapper.KernelSettings.SystemMessageName).Replace(TestGeneratorsConstants.GenerateRetriesNumberPlaceholder, _options.ScriptFixRetries.ToString()));
        history.AddUserMessage(generateTestRequest.ToUserMessage());
        var promptExecutionSettings = CreatePromptExecutionSettings(kernelWrapper);
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
        var response = await chatClient.GetChatMessageContentsAsync(history, promptExecutionSettings, kernelWrapper.Kernel, cancellationToken);
        ArgumentNullException.ThrowIfNull(response);
        if (response.Count == 0)
        {
            throw new SemanticKernelException($"No response received from the chat client. (response.Count==0)");
        }
        var assistantMessages = history.Where(m => m.Role == AuthorRole.Assistant && m is OpenAIChatMessageContent).Cast<OpenAIChatMessageContent>();
        var toolCallToPlaywrightTestScriptPlugin = assistantMessages.Where(m => m.ToolCalls.Select(t => t.FunctionName).Contains($"{nameof(PlaywrightTestScriptPlugin)}-{PlaywrightTestScriptPlugin.KernelFunctionName}")).ToList();
        var testScript = "";
        bool hasToolCallToPlaywrightTestScriptPlugin=false;
        if (toolCallToPlaywrightTestScriptPlugin.Count > 0)
        {
            hasToolCallToPlaywrightTestScriptPlugin = true;
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(toolCallToPlaywrightTestScriptPlugin.Last().ToolCalls.First()?.FunctionArguments);
            ArgumentNullException.ThrowIfNull(dict);
            testScript = dict.First().Value;
        }
        var errorContent = string.Empty;
        var testPass = response[response.Count - 1].Content?.StartsWith("TEST OK", StringComparison.OrdinalIgnoreCase) ?? false;
        if (!testPass)
        {
            var errorFiles = Directory.GetFiles("./test-results", "error-context.md", SearchOption.AllDirectories);
            if (errorFiles.Length > 0)
            {
                errorContent = await File.ReadAllTextAsync(errorFiles[0]);
            }
        }
        return new GenerateTestResult
        {
            Id = generateTestRequest.Id,
            Text = response[response.Count - 1].Content ?? "",
            TestScript = testScript,
            ScriptAvailable = hasToolCallToPlaywrightTestScriptPlugin,
            TestPass = testPass,
            ErrorContent = errorContent,
        };

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
