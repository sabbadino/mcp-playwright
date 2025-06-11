
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Testing.Platform.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using playwright.test.generator.Abstractions;
using playwright.test.generator.Services;
using Xunit;

namespace playwright.test.generator.TestProject
{
    public class UnitTest1
    {
        private readonly IServiceProvider _serviceProvider;  
        public UnitTest1()
        {
            var builder = Host.CreateApplicationBuilder(Environment.GetCommandLineArgs());
            builder.Configuration
                .AddJsonFile("testAppsettings.json", optional: false, reloadOnChange: false)  
                .AddUserSecrets<UnitTest1>()
                .AddEnvironmentVariables()
                .AddCommandLine(Environment.GetCommandLineArgs());

            builder.Services.AddPlayWrightTestGeneratorOptions(options => builder.Configuration.Bind("PlayWrightTestGenerator", options));
            //builder.Logging.ClearProviders();

            //builder.Services.AddOpenTelemetry()
            //  .WithTracing(b =>
            //  {
            //      b.AddHttpClientInstrumentation().AddConsoleExporter()
            //      .AddSource("playwright.test.generator.TestProject")
            //      .SetSampler(new AlwaysOnSampler());
            //  }).WithLogging(builder =>
            //  {
            //      builder.AddConsoleExporter();
            //  });


            var app = builder.Build();
            _serviceProvider = app.Services;
            var logger = _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UnitTest1>>();
            logger.LogInformation("Starting Playwright test generation runner.");
            var runner = _serviceProvider.GetRequiredService<ICommandRunnerService>();
            runner.SetupPlayWright(runner, logger).Wait();
        }

        [Fact]
        public async Task Scenario1Test()
        {
            //var req = new GenerateTestRequest
        //{
        //    Id = "test-id-123",
        //    Name = "Test Scenario",
        //    Description = "This is a test scenario for Playwright test generation.",
        //    Steps = new List<ScenarioStep>
        //        {
        //            new ScenarioStep {StepType = StepType.Given,  Text = "you open  'https://executeautomation.github.io/mcp-playwright/docs/intro'" },
        //            new ScenarioStep {StepType = StepType.Then, Text = "verify that there is a link with exact text 'Release Notes' present on the sidebar"},
        //            new ScenarioStep {StepType = StepType.Given, Text = "you click on 'Release Notes' link"},
        //            new ScenarioStep {StepType = StepType.Given, Text = "you click on 'Version 1.0.3' link"},
        //            new ScenarioStep {StepType = StepType.Then, Text = "verify the page contains the text 'start_codegen_session: Start a new session to record Playwright actions'"},
        //            new ScenarioStep {StepType = StepType.Then, Text = "verify the page contains the text 'end_codegen_session: End a session and generate test file'"},
        //            new ScenarioStep {StepType = StepType.Then, Text = "verify the page contains the text 'get_codegen_session: Retrieve information about a session'"}
        //        },
        //};
        //    var t = JsonSerializer.Serialize(req, new JsonSerializerOptions { WriteIndented = true ,
        //        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)    }
        //    });
            var req = ScenarioLoader.LoadScenario("scenario1.json");    
            var playWrightTestGenerator = _serviceProvider.GetRequiredService<IPlayWrightTestGenerator>();
            var res = await playWrightTestGenerator.GenerateTestIChatClientCompletion(req, CancellationToken.None);

            Assert.True(res.TestPass,res.LLMFinalOutput);
            Assert.True(res.ScriptAvailable, res.LLMFinalOutput);
            Assert.Equal(res.Id, req.Id);
            Assert.True(!string.IsNullOrWhiteSpace(res.TestScript), res.LLMFinalOutput);
            Assert.True(!string.IsNullOrWhiteSpace(res.InputPrompt), res.LLMFinalOutput);

        }
    }
}
