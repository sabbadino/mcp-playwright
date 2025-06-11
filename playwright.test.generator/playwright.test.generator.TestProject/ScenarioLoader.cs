using System.Text.Json;
using System.Text.Json.Serialization;
using playwright.test.generator.Abstractions;
using playwright.test.generator.Services;

namespace playwright.test.generator.TestProject
{
    internal static class ScenarioLoader
    {
        private const string TemplateNamespace = "playwright.test.generator.TestProject.Scenarios";
        private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)    }
            };
        public static GenerateTestRequest LoadScenario(string name)
        {
            var resourceName = $"{TemplateNamespace}.{name}";
            using var stream = typeof(ScenarioLoader).Assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                throw new SemanticKernelException($"Could not find resource {resourceName}");
            }
            using (var reader = new StreamReader(stream))
            {
                var ret = JsonSerializer.Deserialize<GenerateTestRequest>(reader.ReadToEnd(), JsonSerializerOptions);
                if( ret == null)
                {
                    throw new Exception($"Could not deserialize resource {resourceName} to GenerateTestRequest string read from resource : {reader.ReadToEnd()}");
                }   
                return ret;
            }
        }
    }
}
