using System;
using System.Threading;
using System.Threading.Tasks;
using AutoGen.DotnetInteractive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.Extensions.Logging;

namespace CodeExecutionPlugin.plugins;

public class DotNetInteractivePlugin : IDisposable
{
    private readonly DotNetInteractivePluginOptions _options;
    private readonly ILogger<DotNetInteractivePlugin> _logger;
    private InteractiveService _interactiveService;
    private bool _disposed;

    public DotNetInteractivePlugin(DotNetInteractivePluginOptions options, ILoggerFactory loggerFactory)
    {
        _options = options;
        _logger = loggerFactory.CreateLogger<DotNetInteractivePlugin>();

        if (!_options.UseDocker)
        {
            // Initialize in-process kernel
            var builder = new InProcessDotnetInteractiveKernelBuilder();

            // Add the kernels
            builder.AddCSharpKernel();
            builder.AddFSharpKernel();
            builder.AddPowerShellKernel();

            // Check if Python is enabled and add the Python kernel
            if (!string.IsNullOrEmpty(_options.PythonKernelSpecPath))
            {
                builder.AddPythonKernel(_options.PythonKernelSpecPath, _options.PythonKernelName);
            }

            var kernel = builder.Build();
            _interactiveService = new InteractiveService(kernel);
        }
        else
        {
            // Initialize Docker client or Docker-based execution
            throw new NotImplementedException("Docker execution is not implemented yet.");
        }
    }

    // Existing methods: ExecuteCSharpCodeAsync, ExecuteFSharpCodeAsync, ExecutePowerShellCodeAsync
    public async Task<string> ExecuteCSharpCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (_options.UseDocker)
        {
            return await ExecuteCodeInDockerAsync(code, "csharp", cancellationToken);
        }
        else
        {
            var result = await _interactiveService.SubmitCSharpCodeAsync(code, cancellationToken);
            return result ?? "No output.";
        }
    }

    public async Task<string> ExecuteFSharpCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (_options.UseDocker)
        {
            return await ExecuteCodeInDockerAsync(code, "fsharp", cancellationToken);
        }
        else
        {
            var result = await _interactiveService.SubmitCommandAsync(new SubmitCode(code, "fsharp"), cancellationToken);
            return result ?? "No output.";
        }
    }

    public async Task<string> ExecutePowerShellCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (_options.UseDocker)
        {
            return await ExecuteCodeInDockerAsync(code, "pwsh", cancellationToken);
        }
        else
        {
            var result = await _interactiveService.SubmitPowerShellCodeAsync(code, cancellationToken);
            return result ?? "No output.";
        }
    }

    public async Task<string> ExecutePythonCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (_options.UseDocker)
        {
            return await ExecuteCodeInDockerAsync(code, "python", cancellationToken);
        }
        else
        {
            var command = new SubmitCode(code, targetKernelName: _options.PythonKernelName);
            var result = await _interactiveService.SubmitCommandAsync(command, cancellationToken);
            return result ?? "No output.";
        }
    }

    private async Task<string> ExecuteCodeInDockerAsync(string code, string language, CancellationToken cancellationToken)
    {
        // Implement Docker-based code execution
        throw new NotImplementedException("Docker execution is not implemented yet.");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _interactiveService?.Dispose();
            _disposed = true;
        }
    }
}
