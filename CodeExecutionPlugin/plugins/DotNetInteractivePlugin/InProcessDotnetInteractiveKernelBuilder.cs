using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PackageManagement;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Jupyter;

namespace CodeExecutionPlugin.plugins;

/// <summary>
/// Build an in-proc dotnet interactive kernel.
/// </summary>
public class InProcessDotnetInteractiveKernelBuilder
{
    private readonly CompositeKernel compositeKernel;

    internal InProcessDotnetInteractiveKernelBuilder()
    {
        this.compositeKernel = new CompositeKernel();

        // Add Jupyter connectors if necessary
        this.compositeKernel.AddKernelConnector(
            new ConnectJupyterKernelCommand()
                .AddConnectionOptions(new JupyterLocalKernelConnectionOptions()));
    }

    public InProcessDotnetInteractiveKernelBuilder AddCSharpKernel(IEnumerable<string>? aliases = null)
    {
        aliases ??= new[] { "c#", "C#", "csharp" };
        // create csharp kernel
        var csharpKernel = new CSharpKernel()
            .UseNugetDirective((k, resolvedPackageReferences) =>
            {
                k.AddAssemblyReferences(resolvedPackageReferences
                    .SelectMany(r => r.AssemblyPaths));
                return Task.CompletedTask;
            })
            .UseKernelHelpers()
            .UseWho()
            .UseValueSharing();

        this.AddKernel(csharpKernel, aliases);

        return this;
    }

    public InProcessDotnetInteractiveKernelBuilder AddFSharpKernel(IEnumerable<string>? aliases = null)
    {
        aliases ??= new[] { "f#", "F#", "fsharp" };
        // create fsharp kernel
        var fsharpKernel = new FSharpKernel()
            .UseDefaultFormatting()
            .UseKernelHelpers()
            .UseWho()
            .UseValueSharing();

        this.AddKernel(fsharpKernel, aliases);

        return this;
    }

    public InProcessDotnetInteractiveKernelBuilder AddPowerShellKernel(IEnumerable<string>? aliases = null)
    {
        aliases ??= new[] { "pwsh", "powershell" };
        // create powershell kernel
        var powershellKernel = new PowerShellKernel()
                .UseProfiles()
                .UseValueSharing();

        this.AddKernel(powershellKernel, aliases);

        return this;
    }

    public InProcessDotnetInteractiveKernelBuilder AddPythonKernel(string venv, string kernelName = "python")
    {
        // Create Python kernel by connecting to Jupyter
        var magicCommand = $"#!connect jupyter --kernel-name {kernelName} --kernel-spec {venv}";
        var connectCommand = new SubmitCode(magicCommand);
        var result = this.compositeKernel.SendAsync(connectCommand).Result;

        //result.ThrowOnCommandFailed();

        return this;
    }

    public CompositeKernel Build()
    {
        // Adjust the Build method if UseDefaultMagicCommands does not exist
        // Remove or replace UseDefaultMagicCommands()

        // Option 1: Remove UseDefaultMagicCommands() if it's not available
        return this.compositeKernel
            //.UseDefaultMagicCommands() // Remove this line if the method doesn't exist
            .UseImportMagicCommand();
    }

    private InProcessDotnetInteractiveKernelBuilder AddKernel(Kernel kernel, IEnumerable<string>? aliases = null)
    {
        this.compositeKernel.Add(kernel, aliases);
        return this;
    }
}
