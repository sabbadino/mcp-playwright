namespace playwright.test.generator.Abstractions
{
    public interface IPlayWrightTestGenerator
    {
   //     Task<GenerateTestResult> GenerateTestIChatClient(GenerateTestRequest generateTestRequest, CancellationToken cancellationToken = default);
        Task<GenerateTestResult> GenerateTestIChatClientCompletion(GenerateTestRequest generateTestRequest, CancellationToken cancellationToken = default);
    }
}
