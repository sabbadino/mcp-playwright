

using System.Text.Json;
using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OllamaSharp;
using playwright.test.generator;
using playwright.test.generator.Abstractions;
using playwright.test.generator.IocConventions;
using playwright.test.generator.Services;
using static System.Net.Mime.MediaTypeNames;
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.AddPlayWrightTestGeneratorOptions(options => builder.Configuration.Bind("PlayWrightTestGenerator", options));


var app = builder.Build();

var playWrightTestGenerator = app.Services.GetRequiredService<IPlayWrightTestGenerator>();
var res = await playWrightTestGenerator.GenerateTest(new GenerateTestRequest
{
    Id = "test-id-123",
    Name = "Test Scenario",
    Description = "This is a test scenario for Playwright test generation.",
    Steps = new List<ScenarioStep>
    {
        new ScenarioStep {StepType = StepType.Given,  Text = "you open  'https://executeautomation.github.io/mcp-playwright/docs/intro'" },
        new ScenarioStep {StepType = StepType.Then, Text = "verify that the link has text 'Release Notes' text is present on the page"},
        new ScenarioStep {StepType = StepType.Given, Text = "you click on 'Release Notes' link "},
        new ScenarioStep {StepType = StepType.Given, Text = "you click on 'Version 1.0.3' link"},
        new ScenarioStep {StepType = StepType.Then, Text = "verify the page contains the text 'start_codegen_session: Start a new session to record Playwright actions'"},
        new ScenarioStep {StepType = StepType.Then, Text = "verify the page contains the text 'end_codegen_session: End a session and generate test file'"},
        new ScenarioStep {StepType = StepType.Then, Text = "verify the page contains the text 'get_codegen_session: Retrieve information about a session'"}
    },  
});

Console.WriteLine(JsonSerializer.Serialize(res));  


