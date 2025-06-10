

using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using playwright.test.generator;
using playwright.test.generator.Abstractions;
using playwright.test.generator.IocConventions;
using playwright.test.generator.Services; 
using static System.Net.Mime.MediaTypeNames;





HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration 
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.AddPlayWrightTestGeneratorOptions(options => builder.Configuration.Bind("PlayWrightTestGenerator", options));
builder.Logging.AddConsole();
//builder.Logging.ClearProviders();

//builder.Services.AddOpenTelemetry()
//  .WithTracing(b =>
//  {
//      b.AddHttpClientInstrumentation().AddConsoleExporter()
//       .SetResourceBuilder(ResourceBuilder
//                .CreateDefault()
//                .AddService("ConsoleApp")) 
//      .AddSource("playwright.test.generator.runner")
//      .SetSampler(new AlwaysOnSampler());
//  }).WithLogging(builder =>
//  {
//      builder.AddConsoleExporter();
//  });


var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Playwright test generation runner.");   
var runner= app.Services.GetRequiredService<ICommandRunnerService>();
await runner.SetupPlayWright(runner, logger);
var playWrightTestGenerator = app.Services.GetRequiredService<IPlayWrightTestGenerator>();
var res = await playWrightTestGenerator.GenerateTestIChatClient(new GenerateTestRequest
{
    Id = "test-id-123",
    Name = "Test Scenario",
    Description = "This is a test scenario for Playwright test generation.",
    Steps = new List<ScenarioStep>
    {
        new ScenarioStep {StepType = StepType.Given,  Text = "you open  'https://executeautomation.github.io/mcp-playwright/docs/intro'" },
        new ScenarioStep {StepType = StepType.Then, Text = "verify that there is a link with exact text 'Release Notes' present on the sidebar"},
        new ScenarioStep {StepType = StepType.Given, Text = "you click on 'Release Notes' link"},
        new ScenarioStep {StepType = StepType.Given, Text = "you click on 'Version 1.0.3' link"},
        new ScenarioStep {StepType = StepType.Then, Text = "verify the page contains the text 'start_codegen_session: Start a new session to record Playwright actions'"},
        new ScenarioStep {StepType = StepType.Then, Text = "verify the page contains the text 'end_codegen_session: End a session and generate test file'"},
        new ScenarioStep {StepType = StepType.Then, Text = "verify the page contains the text 'get_codegen_session: Retrieve information about a session'"}
    },  
});

logger.LogInformation($"run result: {JsonSerializer.Serialize(res)}");




