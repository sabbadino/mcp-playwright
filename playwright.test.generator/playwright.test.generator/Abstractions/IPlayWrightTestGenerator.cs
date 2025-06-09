namespace playwright.test.generator.Abstractions
{
    public interface IPlayWrightTestGenerator
    {
        Task<GenerateTestResult> GenerateTest(GenerateTestRequest generateTestRequest, CancellationToken cancellationToken = default);
    }
}
