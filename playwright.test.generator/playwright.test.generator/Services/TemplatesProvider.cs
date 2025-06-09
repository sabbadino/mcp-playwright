using System.IO;
using Microsoft.Extensions.Caching.Memory;
using playwright.test.generator.IocConventions;


namespace playwright.test.generator.Services
{

    public interface ITemplatesProvider
    {
        string GetTemplate(string name);
    }
    public class TemplatesProvider : ITemplatesProvider, ISingletonScope
    {
        private const string TemplateNamespace = "playwright.test.generator.Templates";   

        public string GetTemplate(string name)
        {
            var resourceName = $"{TemplateNamespace}.{name}";
            using var stream = typeof(TemplatesProvider).Assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                throw new SemanticKernelException($"Could not find resource {resourceName}");
            }
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
