﻿using System.Diagnostics;
using Microsoft.Extensions.Logging;
using playwright.test.generator.IocConventions;

namespace playwright.test.generator.Services;

public interface ICommandRunnerService
{
    Task<(int ExitCode, string stdOut, string stdError)> RunCommand(string commandToRun);
    Task SetupPlayWright(ICommandRunnerService commandRunnerService, ILogger logger);
}

public class CommandRunnerService : ICommandRunnerService, ISingletonScope
{
    private readonly ILogger<CommandRunnerService> _logger;

    public CommandRunnerService(ILogger<CommandRunnerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }   
    public async Task<(int ExitCode, string stdOut, string stdError)> RunCommand(string commandToRun)
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
        if (process == null)
        {
            throw new Exception($"Process should not be null. filename {processStartInfo.FileName} arguments {processStartInfo.Arguments}");
        }
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

       

        process.StandardInput.WriteLine($"{commandToRun} & exit");
        await Task.WhenAll(outputTask, errorTask);
        await process.WaitForExitAsync();

        if(process.ExitCode != 0)
        {
            _logger.LogWarning($"Command '{commandToRun}' failed with exit code {process.ExitCode}. Output: {outputTask.Result}, Error: {errorTask.Result}");
        }
        else {
            _logger.LogInformation($"exit code {process.ExitCode}. Output: {outputTask.Result}");
        }
        return (process.ExitCode, outputTask.Result, errorTask.Result); // Return the exit code of the process 
    }

    public async Task SetupPlayWright(ICommandRunnerService commandRunnerService, ILogger logger)
    {
        try
        {

            var ec = await commandRunnerService.RunCommand("npm.cmd init -y");
            if (ec.ExitCode != 0)
            {
                throw new Exception($"npm.cmd init -y failed with exit code {ec}");
            }
            // Install Playwright cli 
            ec = await commandRunnerService.RunCommand("npm.cmd install --save-dev @playwright/test");
            if (ec.ExitCode != 0)
            {
                throw new Exception($"npm install @playwright/test failed with exit code {ec}");
            }
            // Install browser plugins 
            ec = await commandRunnerService.RunCommand("npx.cmd playwright install");
            if (ec.ExitCode != 0)
            {
                throw new Exception($"npx.cmd playwright install failed with exit code {ec}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"A generic error occurred while setting up Playwright. {ex}");
            throw;
        }
    }
}
