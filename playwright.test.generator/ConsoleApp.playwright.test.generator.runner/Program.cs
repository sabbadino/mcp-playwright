

using System.Diagnostics;
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


await SetupPlayWright();


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration 
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

Console.WriteLine($"run result: {JsonSerializer.Serialize(res)}");

async Task SetupPlayWright() {
    try
    {

        var ec = await RunCommand("npm.cmd init -y");
        Console.WriteLine($"npm init exit code: {ec}"); 
        ec = await RunCommand("npm.cmd install --save-dev @playwright/test");
        Console.WriteLine($"npm install @playwright/test exit code: {ec}");

        ec = await RunCommand("npm.cmd install --save-dev @playwright/test");
        Console.WriteLine($"npm install @playwright/test exit code: {ec}");
        ec = await RunCommand("npx.cmd playwright install");
        Console.WriteLine($"npx playwright install exit code: {ec}");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while executing the Playwright test script: {ex.ToString()}");
    }
}

static async Task<int> RunCommand(string commandToRun)
{
    var workingDirectory = Directory.GetCurrentDirectory();

    var processStartInfo = new ProcessStartInfo()
    {
        FileName = "cmd",
        RedirectStandardOutput = true,
        RedirectStandardInput = true,
        RedirectStandardError = true,
        WorkingDirectory = workingDirectory
    };

    var process = Process.Start(processStartInfo);

    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
    Task<string> errorTask = process.StandardError.ReadToEndAsync();

    if (process == null)
    {
        throw new Exception("Process should not be null.");
    }

    process.StandardInput.WriteLine($"{commandToRun} & exit");
    await Task.WhenAll(outputTask, errorTask);
    await process.WaitForExitAsync();

    Console.WriteLine($"out {outputTask.Result} error: {errorTask.Result}");
    return process.ExitCode; // Return the exit code of the process 
}
