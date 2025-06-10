using System.Diagnostics;
using Microsoft.Extensions.Logging;
using playwright.test.generator.IocConventions;

namespace playwright.test.generator.Services;

public interface ICommandRunnerService
{
    Task<(int ExitCode, string stdOut, string stdError)> RunCommand(string commandToRun);
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
}
