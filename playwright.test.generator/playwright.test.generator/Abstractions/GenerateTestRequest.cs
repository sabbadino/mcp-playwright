using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace playwright.test.generator.Abstractions
{
    public record GenerateTestRequest
    {
        public required string Id { get; init; } = "";
      
        public string Description { get; init; } = "";

        public List<ScenarioStep> Steps { get; init; } = new List<ScenarioStep>();

        public string KernelName { get; init; } = "";

       
    }

    public record  ScenarioStep
    {
        public StepType StepType { get; init; } = StepType.Given;
        public string Text { get; init; } = "";
    }

    public record GenerateTestResult
    {
        public required string LLMFinalOutput { get; init; } = "";
        public required string TestScript { get; init; } = "";
        public required  bool ScriptAvailable { get; init; }
        public required string Id { get; init; }
        public required bool TestPass { get; init; }
        public string? ErrorContent { get; init; }

        public required string InputPrompt { get; init; } = "";
    }
}
