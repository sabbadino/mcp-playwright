using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;


namespace playwright.test.generator.Settings
{
    public class PlayWrightTestGeneratorOptions 
    {
        public SemanticKernelsSettings? SemanticKernelsSettings { get; init; }
    }
    public class SemanticKernelsSettings
    {
        

        public List<KernelSettings> KernelSettings { get; init; } = new();

        public Dictionary<string, string> ApiKeys { get; init; } = new();
        public LogLevel? LogLevel { get; internal set; }
    }
    public class KernelSettings
    {
        public List<string> Plugins { get; init; } = new();
        public string SystemMessageName { get; init; } = "";
        public bool IsDefault { get; init; } = false;
        public string Name { get; init; } = "";
        public int Temperature { get; init; } = 0;
        public Model? Model { get; init; }


    }


    public class Model
    {
        public bool RequiresUrl { get; init; } = true;

        public ModelCategory? Category { get; init; }

        public string DeploymentOrModelName { get; init; } = "";

        public string Url { get; init; } = "";

        public string ApiKeyName { get; init; } = "";
    }

    public enum ModelCategory
    {
        None,
        OpenAi,
        AzureOpenAi,
        Gemini
    }
}
