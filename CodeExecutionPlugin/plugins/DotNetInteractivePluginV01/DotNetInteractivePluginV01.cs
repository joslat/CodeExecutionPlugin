// DotNetInteractivePlugin.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.PackageManagement;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using CodeExecutionPlugin.plugins;

namespace CodeExecutionPlugin.plugins;

public class DotNetInteractivePluginV01 : IDisposable
{
    private readonly DotNetInteractivePluginV01Options _options;
    private readonly ILogger<DotNetInteractivePluginV01> _logger;

    // In-process kernel
    private readonly CompositeKernel _kernel;

    // Docker client
    private readonly DockerClient _dockerClient;

    public DotNetInteractivePluginV01(DotNetInteractivePluginV01Options options, ILoggerFactory loggerFactory)
    {
        _options = options;
        _logger = loggerFactory.CreateLogger<DotNetInteractivePluginV01>();

        if (!_options.UseDocker)
        {
            // Define the onResolvePackageReferences function for CSharpKernel
            Func<CSharpKernel, IReadOnlyList<ResolvedPackageReference>, Task> csharpPackageHandler = async (kernel, references) =>
            {
                if (references == null || !references.Any())
                {
                    _logger.LogError("No packages were resolved.");
                    throw new Exception("Package restore failed or no packages were resolved.");
                }

                foreach (var reference in references)
                {
                    kernel.AddAssemblyReferences(references.SelectMany(r => r.AssemblyPaths));
                }
            };

            // Initialize in-process kernel
            _kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(csharpPackageHandler, forceRestore: true),
                new FSharpKernel(),
                new PowerShellKernel()
            };
            _kernel.DefaultKernelName = "csharp";
        }
        else
        {
            // Initialize Docker client
            _dockerClient = new DockerClientConfiguration(new Uri(_options.DockerEndpoint)).CreateClient();
        }
    }

    [KernelFunction]
    [Description("Executes the specified C# code.")]
    [return: Description("The result of the code execution.")]
    public async Task<string> ExecuteCSharpCode(KernelArguments arguments)
    {
        string code = arguments["input"]?.ToString() ?? throw new ArgumentException("No code provided in 'input' argument.");
        if (_options.UseDocker)
        {
            return await ExecuteCodeInDocker(code, "csharp");
        }
        else
        {
            return await ExecuteCodeInProcess(code, "csharp");
        }
    }

    [KernelFunction]
    [Description("Executes the specified F# code.")]
    [return: Description("The result of the code execution.")]
    public async Task<string> ExecuteFSharpCode(KernelArguments arguments)
    {
        string code = arguments["input"]?.ToString() ?? throw new ArgumentException("No code provided in 'input' argument.");
        if (_options.UseDocker)
        {
            return await ExecuteCodeInDocker(code, "fsharp");
        }
        else
        {
            return await ExecuteCodeInProcess(code, "fsharp");
        }
    }

    [KernelFunction]
    [Description("Executes the specified PowerShell code.")]
    [return: Description("The result of the code execution.")]
    public async Task<string> ExecutePowerShellCode(KernelArguments arguments)
    {
        string code = arguments["input"]?.ToString() ?? throw new ArgumentException("No code provided in 'input' argument.");
        if (_options.UseDocker)
        {
            return await ExecuteCodeInDocker(code, "pwsh");
        }
        else
        {
            return await ExecuteCodeInProcess(code, "pwsh");
        }
    }

    [KernelFunction]
    [Description("Executes the specified Python code.")]
    [return: Description("The result of the code execution.")]
    public async Task<string> ExecutePythonCode(KernelArguments arguments)
    {
        string code = arguments["input"]?.ToString() ?? throw new ArgumentException("No code provided in 'input' argument.");
        if (_options.UseDocker)
        {
            return await ExecutePythonCodeInDocker(code);
        }
        else
        {
            throw new NotSupportedException("In-process execution of Python code is not supported.");
        }
    }

    private async Task<string> ExecuteCodeInProcess(string code, string language)
    {
        if (_kernel == null)
        {
            throw new InvalidOperationException("In-process kernel is not initialized.");
        }

        //var targetKernel = _kernel.FindKernel(language);
        var targetKernel = _kernel.FindKernelByName(language);

        if (targetKernel == null)
        {
            throw new InvalidOperationException($"Kernel for language '{language}' not found.");
        }

        var result = new System.Text.StringBuilder();
        //using var events = targetKernel.KernelEvents.ToSubscribedList();
        var events = new List<KernelEvent>();
        using var subscription = targetKernel.KernelEvents.Subscribe(e => events.Add(e));

        await targetKernel.SendAsync(new SubmitCode(code));

        foreach (var ev in events)
        {
            switch (ev)
            {
                case CommandSucceeded _:
                    break;
                case CommandFailed cf:
                    _logger.LogError(cf.Exception, "Code execution failed.");
                    return cf.Message;
                case StandardOutputValueProduced so:
                    result.AppendLine(so.FormattedValues.FirstOrDefault()?.Value);
                    break;
                case StandardErrorValueProduced se:
                    result.AppendLine(se.FormattedValues.FirstOrDefault()?.Value);
                    break;
                case ReturnValueProduced rv:
                    result.AppendLine(rv.FormattedValues.FirstOrDefault()?.Value);
                    break;
            }
        }

        return result.ToString();
    }

    private async Task<string> ExecuteCodeInDocker(string code, string language)
    {
        // Create a temporary directory to hold the code
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Write code to a file
            string fileName = language switch
            {
                "csharp" => "Program.cs",
                "fsharp" => "Program.fs",
                "pwsh" => "Script.ps1",
                _ => throw new ArgumentException("Unsupported language.")
            };

            var codeFilePath = Path.Combine(tempDir, fileName);
            File.WriteAllText(codeFilePath, code);

            // Prepare Docker container
            string imageName = _options.DockerImage; // Use .NET SDK image
            await PullDockerImageAsyncV2(imageName);

            // Set up volume bindings
            //var bind = new string[] { $"{tempDir}:{_options.WorkingDirectory}" };
            var bind = new string[] { $"{tempDir.Replace("\\", "/")}:{_options.WorkingDirectory}" };

            Console.WriteLine($"Host temp directory: {tempDir}");
            Console.WriteLine($"Container working directory: {_options.WorkingDirectory}");

            // Set up commands
            var commands = language switch
            {
                //"csharp" => new[] { "dotnet", "run", "--project", $"{_options.WorkingDirectory}" },
                //"csharp" => new[] { "dotnet", "script", $"{_options.WorkingDirectory}/Program.cs" },
                //"csharp" => new[] { "sh", "-c", $"cd {_options.WorkingDirectory} && dotnet script Program.cs" },
                //"csharp" => new[] { "sh", "-c", $"cat {_options.WorkingDirectory}/Program.cs && dotnet run --project {_options.WorkingDirectory}" },
                "csharp" => new[] { "sh", "-c", "dotnet tool install -g dotnet-script && export PATH=\"$PATH:/root/.dotnet/tools\" && dotnet script Program.cs" },

                "fsharp" => new[] { "dotnet", "run", "--project", $"{_options.WorkingDirectory}" },
                "pwsh" => new[] { "pwsh", $"{_options.WorkingDirectory}/Script.ps1" },
                _ => throw new ArgumentException("Unsupported language.")
            };

            commands = new[]
            {
                "sh",
                "-c",
                //"echo I am here..",
                //$"cat {_options.WorkingDirectory}/Program.cs",
                $"dotnet new console -o {_options.WorkingDirectory} --force && " +// Create a new console project
                //$"mv {_options.WorkingDirectory}/Program.cs {_options.WorkingDirectory}/Program/Program.cs && " +  // Move the generated Program.cs to the project
                $"dotnet run --project {_options.WorkingDirectory}/"  // Run the project
            };


            // Create and start container
            var containerId = await CreateAndStartContainer(imageName, bind, commands);

            // Get output
            var output = await GetContainerOutput(containerId);

            // Clean up
            await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true });

            return output;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return String.Empty;
        }
        finally
        {
            // Clean up temp directory
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    private async Task<string> ExecutePythonCodeInDocker(string code)
    {
        // Create a temporary directory to hold the code
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Write code to a file
            var codeFilePath = Path.Combine(tempDir, "script.py");
            File.WriteAllText(codeFilePath, code);

            // Prepare Docker container
            string imageName = _options.PythonDockerImage;
            await PullDockerImageAsync(imageName);

            // Set up volume bindings
            var bind = new string[] { $"{tempDir}:{_options.WorkingDirectory}" };

            // Set up commands
            var commands = new[] { "python", $"{_options.WorkingDirectory}/script.py" };

            // Create and start container
            var containerId = await CreateAndStartContainer(imageName, bind, commands);

            // Get output
            var output = await GetContainerOutput(containerId);

            // Clean up
            await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true });

            return output;
        }
        finally
        {
            // Clean up temp directory
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    private async Task PullDockerImageAsync(string imageName)
    {
        try
        {
            // Check if image exists
            var response = await _dockerClient.Images.InspectImageAsync(imageName);
        }
        catch (DockerImageNotFoundException)
        {
            // Pull image
            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = imageName },
                null,
                new Progress<JSONMessage>());
        }
    }

    private async Task PullDockerImageAsyncV2(string imageName)
    {
        try
        {
            // Check if image exists locally
            await _dockerClient.Images.InspectImageAsync(imageName);
        }
        catch (DockerImageNotFoundException)
        {
            _logger.LogInformation($"Image {imageName} not found locally. Attempting to pull...");

            try
            {
                // Pull the image from Docker Hub
                await _dockerClient.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = imageName },
                    null,
                    new Progress<JSONMessage>(message =>
                    {
                        if (!string.IsNullOrEmpty(message.Status))
                        {
                            _logger.LogInformation($"Pulling image: {message.Status}");
                        }
                    })
                );
                _logger.LogInformation($"Image {imageName} pulled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to pull Docker image: {imageName}");
                throw new Exception($"Failed to pull Docker image: {imageName}", ex);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection to Docker failed. Ensure Docker is running and accessible.");
            throw new Exception("Connection to Docker daemon failed. Ensure Docker is running.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error while inspecting Docker image: {imageName}");
            throw new Exception($"Error while inspecting Docker image: {imageName}", ex);
        }
    }


    private async Task<string> CreateAndStartContainer(string imageName, string[] binds, string[] commands)
    {
        var createParams = new CreateContainerParameters
        {
            Image = imageName,
            Tty = false,
            HostConfig = new HostConfig
            {
                Binds = binds,
                NetworkMode = "bridge", // Disable networking for security
                ReadonlyRootfs = false, // allow writing to the container's file system
                Memory = 512 * 1024 * 1024, // Limit memory to 256MB
                NanoCPUs = 1_000_000_000 // Limit CPU to 1 core
            },
            Cmd = commands,
            WorkingDir = _options.WorkingDirectory,
            User = "root" //"app" // Use the default non-root app user in .NET 8
        };

        var response = await _dockerClient.Containers.CreateContainerAsync(createParams);
        var containerId = response.ID;

        await _dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());

        return containerId;
    }

    private async Task<string> GetContainerOutput(string containerId)
    {
        var parameters = new ContainerLogsParameters
        {
            ShowStdout = true,
            ShowStderr = true,
            Follow = false,
            Timestamps = false
        };

        var stream = await _dockerClient.Containers.GetContainerLogsAsync(containerId, parameters);

        using var reader = new StreamReader(stream);
        
        // Log both stdout and stderr
        var output = await reader.ReadToEndAsync();
        _logger.LogInformation("Container output: {output}");


        // return await reader.ReadToEndAsync();
        return output;
    }

    public void Dispose()
    {
        _dockerClient?.Dispose();
        _kernel?.Dispose();
    }
}
