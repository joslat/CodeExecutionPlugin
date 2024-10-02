using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;

namespace AutoGen.DotnetInteractive;

public class InteractiveService : IDisposable
{
    private Kernel? kernel = null;
    private bool disposedValue;

    /// <summary>
    /// Create an instance of <see cref="InteractiveService"/> with a running kernel.
    /// When using this constructor, you don't need to call <see cref="StartAsync(string, CancellationToken)"/> to start the kernel.
    /// </summary>
    /// <param name="kernel"></param>
    public InteractiveService(Kernel kernel)
    {
        this.kernel = kernel;
    }

    public Kernel? Kernel => this.kernel;

    public async Task<string?> SubmitCommandAsync(SubmitCode cmd, CancellationToken ct)
    {
        if (this.kernel == null)
        {
            throw new Exception("Kernel is not running");
        }

        return await this.kernel.RunSubmitCodeCommandAsync(cmd.Code, cmd.TargetKernelName, ct);
    }

    public async Task<string?> SubmitPowerShellCodeAsync(string code, CancellationToken ct)
    {
        var command = new SubmitCode(code, targetKernelName: "pwsh");
        return await this.SubmitCommandAsync(command, ct);
    }

    public async Task<string?> SubmitCSharpCodeAsync(string code, CancellationToken ct)
    {
        var command = new SubmitCode(code, targetKernelName: "csharp");
        return await this.SubmitCommandAsync(command, ct);
    }

    public bool IsRunning()
    {
        return this.kernel != null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                this.kernel?.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public static class KernelExtensions
{
    public static async Task<string> RunSubmitCodeCommandAsync(this Kernel kernel, string code, string targetKernelName, CancellationToken ct)
    {
        var result = new System.Text.StringBuilder();
        var events = new System.Collections.Generic.List<KernelEvent>();

        using var subscription = kernel.KernelEvents.Subscribe(e =>
        {
            events.Add(e);

            if (e is StandardOutputValueProduced so)
            {
                result.AppendLine(so.FormattedValues.FirstOrDefault()?.Value);
            }
            else if (e is StandardErrorValueProduced se)
            {
                result.AppendLine(se.FormattedValues.FirstOrDefault()?.Value);
            }
            else if (e is ReturnValueProduced rv)
            {
                result.AppendLine(rv.FormattedValues.FirstOrDefault()?.Value);
            }
            else if (e is CommandFailed cf)
            {
                result.AppendLine($"Error: {cf.Message}");
            }
        });

        var command = new SubmitCode(code, targetKernelName);
        await kernel.SendAsync(command, ct);

        return result.ToString();
    }
}
