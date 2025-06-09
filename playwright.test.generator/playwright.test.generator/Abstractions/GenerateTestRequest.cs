using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace playwright.test.generator.Abstractions
{
    public record GenerateTestRequest
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";

        public List<string> Steps { get; init; } = new List<string>();

        public string KernelName { get; init; } = "";
    }

    public record  ScenarioStep
    {
        public StepType StepType { get; init; } = StepType.Given;
        public string Text { get; init; } = "";
    }

    public record GenerateTestResult { }
}
