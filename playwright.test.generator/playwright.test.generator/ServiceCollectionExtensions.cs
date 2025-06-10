using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using playwright.test.generator.IocConventions;
using playwright.test.generator.Settings;


namespace playwright.test.generator
{


    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Elasticsearch GenerativeAI Functionalities to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="generativeAIOptionsAction">The action to configure GenerativeAI options.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddPlayWrightTestGeneratorOptions(this IServiceCollection services,
            Action<PlayWrightTestGeneratorOptions> playWrightTestGeneratorOptionsAction)
        {
            var playWrightTestGeneratorOptions = new PlayWrightTestGeneratorOptions();
            playWrightTestGeneratorOptionsAction.Invoke(playWrightTestGeneratorOptions);
            return services.AddPlayWrightTestGeneratorOptions(playWrightTestGeneratorOptions);    

        }

        /// <summary>
        /// Adds Elasticsearch GenerativeAi Functionalities to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="playWrightTestGeneratorOptions">The GenerativeAi options.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddPlayWrightTestGeneratorOptions(this IServiceCollection services,
            PlayWrightTestGeneratorOptions? playWrightTestGeneratorOptions)
        {
            // LLM api-keys are not in the appsettings file 
            // if you are running locally put them in the secret files
            // if you are running in docker compose you must set them via env variables
            // or bring the secret file in the container    
            // to add path to user secret file in docker compose for debug ..
            // (not sure it works in linux container) see https://stackoverflow.com/questions/42729723/should-i-use-user-secrets-or-environment-variables-with-docker
            ArgumentNullException.ThrowIfNull(playWrightTestGeneratorOptions);    
            var validator = new SemanticKernelOptionsValidation();
            // I want to trigger validation during setup
            // i will not even inject GenerativeAiOptions in the IOC 
            var validationResult = validator.Validate(null, playWrightTestGeneratorOptions);
            if (validationResult.Failed)
            {
                throw new SemanticKernelConfigurationException($"SemanticKernelOptions validation failed: {string.Join(',', validationResult.Failures)}");
            }

            var kernelsSettings = playWrightTestGeneratorOptions.SemanticKernelsSettings;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            // checked for null by semanticKernelOptionsValidation.Validate
            var allPlugins = kernelsSettings.KernelSettings.SelectMany(k => k.Plugins).Distinct();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            foreach (var namespaceQualifiedClassName in allPlugins)
            {
                // build all plugins of all kernels using the global IOC container
                // to check life time , if it fits for rety logic for retry logic
                var pluginName = namespaceQualifiedClassName.Split('.').Last();
                services.AddKeyedTransient(pluginName, (serviceProvider, _) =>
                {
                    var type = Type.GetType(namespaceQualifiedClassName);
                    ArgumentNullException.ThrowIfNull(type, $"Class {namespaceQualifiedClassName} could not be found");
                    return KernelPluginFactory.CreateFromType(type, pluginName, serviceProvider);
                });
            }
         
           
            foreach (var kernelSetting in kernelsSettings.KernelSettings.Index())
            {
                services.AddTransient(globalServiceProvider =>
                {
                    // create kernel 
                    var skBuilder = Kernel.CreateBuilder();
                    ConfigureKernel(skBuilder, kernelsSettings, kernelSetting.Item);
                    if (kernelsSettings.LogLevel != null)
                    {
                        skBuilder.Services.AddLogging(l => l.SetMinimumLevel(kernelsSettings.LogLevel.Value).AddConsole());
                    }
                    var kernel = skBuilder.Build();

                    // register internal the plugin for this kernel 
                    RegisterKernelPlugins(globalServiceProvider, kernel, kernelSetting.Item.Plugins);

                    // register mcp plugin for this kernel 
                    if (!string.IsNullOrEmpty(kernelSetting.Item.McpServerName)) 
                    { 
                        AddToolsFromMcpClient(globalServiceProvider, kernel, kernelSetting.Index, kernelSetting.Item);
                    }
                    return new KernelWrapper { KernelSettings = kernelSetting.Item, Kernel = kernel };
                });
            }
            services.RegisterByConvention<PlayWrightTestGeneratorOptions>();
            services.AddSingleton(Options.Create(playWrightTestGeneratorOptions));
            return services;
        }

        private static void ConfigureKernel(IKernelBuilder skBuilder, SemanticKernelsSettings semanticKernelsSettings, KernelSettings kernelSettings)
        {
            var model = kernelSettings.Model;
            ArgumentNullException.ThrowIfNull(model);
            string deploymentOrModelName = model.DeploymentOrModelName;
            string? url = model.Url;
            string apiKeyName = model.ApiKeyName;
            var category = model.Category;
            if (!semanticKernelsSettings.ApiKeys.TryGetValue(apiKeyName, out string apiKeyValue))
            {
                throw new Exception($"Could not find key {apiKeyName}");
            }
            if (string.IsNullOrEmpty(apiKeyValue))
            {
                throw new Exception($"value for key {apiKeyName} was found but is null or empty");
            }
            if (category == ModelCategory.AzureOpenAi)
            {
                skBuilder.AddAzureOpenAIChatClient(deploymentOrModelName, url, apiKeyValue);
            }
            else if (category == ModelCategory.OpenAi)
            {
                skBuilder.AddOpenAIChatClient(deploymentOrModelName, apiKeyValue);
            }
            // TODO : add Antropic and google , register as ichatclient (ms.ai.extensions)
//            else if (model.Category == ModelCategory.Gemini)
//            {
//#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//                skBuilder.AddGoogleAIGeminiChatCompletion(model.DeploymentOrModelName, apiKeyValue);
//#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//            }

            
        }

        private static void AddToolsFromMcpClient(IServiceProvider globalServiceProvider,Kernel kernel, int index,KernelSettings kernelSetting)
        {
            var loggerFactory = globalServiceProvider.GetRequiredService<ILoggerFactory>();
            var clientTransport = new StdioClientTransport(new()
            {
                Name = kernelSetting.McpServerName, //"playwright-ms",
                Command = kernelSetting.Command,//"npx",
                Arguments = kernelSetting.Arguments //["@playwright/mcp@latest", "--isolated"],
            });
            var mcpClient = McpClientFactory.CreateAsync(clientTransport, new McpClientOptions{},loggerFactory: loggerFactory).Result;

            var tools = mcpClient.ListToolsAsync().Result;
            //tools = tools.Where(t => mcpPlugins.AcceptedTools.Contains(t.Name) || mcpPlugins.AcceptedTools.Contains("*")).ToList();
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            kernel.Plugins.AddFromFunctions($"{index}", tools.Select(aiFunction => aiFunction.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }

        private static void RegisterKernelPlugins(IServiceProvider globalServiceProvider, Kernel kernel, IEnumerable<string> plugins)
        {
            foreach (var namespaceQualifiedClassName in plugins)
            {
                var pluginName = namespaceQualifiedClassName.Split('.').Last();
                var plugin = globalServiceProvider.GetRequiredKeyedService<KernelPlugin>(pluginName);
                ArgumentNullException.ThrowIfNull(plugin, $"Plugin {pluginName} could not be cast to KernelPlugin");
                kernel.Plugins.Add(plugin);
            }
        }
    }


}
