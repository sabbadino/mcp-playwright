using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
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
            Action<PlayWrightTestGeneratorOptions> generativeAiOptionsAction)
        {
            var generativeAiOptions = new PlayWrightTestGeneratorOptions();
            generativeAiOptionsAction.Invoke(generativeAiOptions);
            return services.AddPlayWrightTestGeneratorOptions(generativeAiOptions);    

        }

        /// <summary>
        /// Adds Elasticsearch GenerativeAi Functionalities to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="generativeAiOptions">The GenerativeAi options.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddPlayWrightTestGeneratorOptions(this IServiceCollection services,
            PlayWrightTestGeneratorOptions? generativeAiOptions)
        {
            // LLM api-keys are not in the appsettings file 
            // if you are running locally put them in the secret files
            // if you are running in docker compose you must set them via env variables
            // or bring the secret file in the container    
            // to add path to user secret file in docker compose for debug ..
            // (not sure it works in linux container) see https://stackoverflow.com/questions/42729723/should-i-use-user-secrets-or-environment-variables-with-docker
            ArgumentNullException.ThrowIfNull(generativeAiOptions);    
            var validator = new SemanticKernelOptionsValidation();
            // I want to trigger validation during setup
            // i will not even inject GenerativeAiOptions in the IOC 
            var validationResult = validator.Validate(null, generativeAiOptions);
            if (validationResult.Failed)
            {
                throw new SemanticKernelConfigurationException($"SemanticKernelOptions validation failed: {string.Join(',', validationResult.Failures)}");
            }

            var kernelsSettings = generativeAiOptions.SemanticKernelsSettings;
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

            foreach (var kernelSetting in kernelsSettings.KernelSettings)
            {
                services.AddTransient(globalServiceProvider =>
                {
                    var skBuilder = Kernel.CreateBuilder();
                    var model = kernelSetting.Model;
                    ArgumentNullException.ThrowIfNull(model);
                    if (!kernelsSettings.ApiKeys.TryGetValue(model.ApiKeyName, out var apiKeyValue))
                    {
                        throw new Exception($"Could not find key {model.ApiKeyName}");
                    }
                    if (string.IsNullOrEmpty(apiKeyValue))
                    {
                        throw new Exception($"value for key {model.ApiKeyName} was found but is null or empty");
                    }
                    if (model.Category == ModelCategory.AzureOpenAi)
                    {
                        skBuilder.AddAzureOpenAIChatCompletion(model.DeploymentOrModelName, model.Url, apiKeyValue);
                    }
                    else if (model.Category == ModelCategory.OpenAi)
                    {
                        skBuilder.AddOpenAIChatCompletion(model.DeploymentOrModelName, apiKeyValue);
                    }
                    else if (model.Category == ModelCategory.AzureOpenAi)
                    {
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                        skBuilder.AddGoogleAIGeminiChatCompletion(model.DeploymentOrModelName, apiKeyValue);
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    }
                    if (kernelsSettings.LogLevel != null)
                    {
                        skBuilder.Services.AddLogging(l => l.SetMinimumLevel(kernelsSettings.LogLevel.Value).AddConsole());
                    }
                    var kernel = skBuilder.Build();
                    // register the plugin for this kernel 
                    foreach (var namespaceQualifiedClassName in kernelSetting.Plugins)
                    {
                        var pluginName = namespaceQualifiedClassName.Split('.').Last();
                        var plugin = globalServiceProvider.GetRequiredKeyedService<KernelPlugin>(pluginName);
                        ArgumentNullException.ThrowIfNull(plugin, $"Plugin {pluginName} could not be cast to KernelPlugin");
                        kernel.Plugins.Add(plugin);
                    }
                    return new KernelWrapper { KernelSettings = kernelSetting, Kernel = kernel };
                });
            }
            services.RegisterByConvention<PlayWrightTestGeneratorOptions>();
            services.AddSingleton(Options.Create(generativeAiOptions));
            return services;
        }
    }
}
