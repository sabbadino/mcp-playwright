
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
    public class UnitTests
    {
        private readonly IServiceProvider _serviceProvider;  
        public UnitTests()
        {
            var builder = Host.CreateApplicationBuilder(Environment.GetCommandLineArgs());
            builder.Configuration
                .AddJsonFile("testAppsettings.json", optional: false, reloadOnChange: false)  
                .AddUserSecrets<UnitTests>()
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
            var logger = _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UnitTests>>();
            logger.LogInformation("Starting Playwright test generation runner.");
            var runner = _serviceProvider.GetRequiredService<ICommandRunnerService>();
            runner.SetupPlayWright(runner, logger).Wait();
        }

        [Fact]
        public async Task Scenario1Test()
        {

            var req = ScenarioLoader.LoadScenario("scenario1.json");
            var playWrightTestGenerator = _serviceProvider.GetRequiredService<IPlayWrightTestGenerator>();
            var res = await playWrightTestGenerator.GenerateTestIChatClientCompletion(req, CancellationToken.None);

            Assert.True(res.TestPass, res.LLMFinalOutput);
            Assert.True(res.ScriptAvailable, res.LLMFinalOutput);
            Assert.Equal(res.Id, req.Id);
            Assert.True(!string.IsNullOrWhiteSpace(res.TestScript), res.LLMFinalOutput);
            Assert.True(!string.IsNullOrWhiteSpace(res.InputPrompt), res.LLMFinalOutput);

        }

            //[Fact]
            //public async Task ScenarioGESANAUC02US02_TC02Test()
            //{

            //    var req = ScenarioLoader.LoadScenario("scenario-GESANA.UC02.US02_TC02.json");
            //    var playWrightTestGenerator = _serviceProvider.GetRequiredService<IPlayWrightTestGenerator>();
            //    var res = await playWrightTestGenerator.GenerateTestIChatClientCompletion(req, CancellationToken.None);

            //    Assert.True(res.TestPass, res.LLMFinalOutput);
            //    Assert.True(res.ScriptAvailable, res.LLMFinalOutput);
            //    Assert.Equal(res.Id, req.Id);
            //    Assert.True(!string.IsNullOrWhiteSpace(res.TestScript), res.LLMFinalOutput);
            //    Assert.True(!string.IsNullOrWhiteSpace(res.InputPrompt), res.LLMFinalOutput);

            //}
        }
}
