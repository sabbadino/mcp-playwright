using Microsoft.SemanticKernel;
namespace playwright.test.generator.Settings
{
    public class KernelWrapper
    {
        public required KernelSettings KernelSettings { get; init; }
        public required Kernel Kernel { get; init; }

    }
}
