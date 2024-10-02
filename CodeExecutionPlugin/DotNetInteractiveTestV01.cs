using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeExecutionPlugin.plugins;

namespace CodeExecutionPlugin;

public static class DotNetInteractiveTestV01
{
    public static async Task Execute()
    {
        Console.WriteLine("Hello from DotNetInteractiveTest");

        var options = new DotNetInteractivePluginV01Options();
        //var options = new DotNetInteractivePluginOptions
        //{
        //    UseDocker = false,
        //    DockerEndpoint = "unix:///var/run/docker.sock",
        //    DockerImage = "mcr.microsoft.com/dotnet/sdk:7.0",
        //    PythonDockerImage = "python:3.10-alpine",
        //    WorkingDirectory = "/workspace"
        //};

        // Set up logging
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<Program>();

        // Instantiate the plugin
        var plugin = new DotNetInteractivePluginV01(options, loggerFactory);

        // Prepare the C# code to execute
        // THIS WONT WORK - needs to be able to resolve nuget packages which seems quite complex.
        // IN AutoGen's NET Implementation this seems to be properly working by controlling the installation of the dotnet interactive tool. See here:
        // https://github.com/microsoft/autogen/blob/main/dotnet/src/AutoGen.DotnetInteractive/InteractiveService.cs
        // (InstallNugetPackages function) and here: https://github.com/microsoft/autogen/blob/main/dotnet/src/AutoGen.DotnetInteractive/DotnetInteractiveFunction.cs
        // Also interesting on how the subkernels are created, one for each language. this is really well done :)
        // see https://github.com/microsoft/autogen/blob/main/dotnet/src/AutoGen.DotnetInteractive/InProccessDotnetInteractiveKernelBuilder.cs
        //string csharpCode = @"
        //#r ""nuget: Newtonsoft.Json, 13.0.1""

        //using Newtonsoft.Json;

        //Console.WriteLine(""Hello, World!"");

        //var obj = new { Name = ""Jane Doe"", Age = 25 };
        //var json = JsonConvert.SerializeObject(obj);
        //Console.WriteLine(json);
        //";

        string csharpCode = @"
        Console.WriteLine(""Hello, World!"");
        ";

        // Create KernelArguments with the code
        var csharpArguments = new KernelArguments
        {
            ["input"] = csharpCode
        };

        // Execute the C# code in DOCKER
        string csharpResult = await plugin.ExecuteCSharpCode(csharpArguments);

        // Output the result
        Console.WriteLine("C# Execution Result:");
        Console.WriteLine(csharpResult);

        var options2 = new DotNetInteractivePluginV01Options
        {
            UseDocker = true,
            DockerEndpoint = "http://localhost:2375",
            DockerImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            PythonDockerImage = "python:3.10-alpine",
            WorkingDirectory = "/workspace"
        };
        var plugin2 = new DotNetInteractivePluginV01(options2, loggerFactory);
        string csharpCode2 = @"
        Console.WriteLine(""Hello, World!"");
        ";
        var csharpArguments2 = new KernelArguments
        {
            ["input"] = csharpCode2
        };
        // Execute the C# code
        string csharpResult2 = await plugin2.ExecuteCSharpCode(csharpArguments2);

        // Output the result
        Console.WriteLine("C# Execution Result:");
        Console.WriteLine(csharpResult2);


        // the Python in-process kernel seems to not work. maybe I need to install the python language? I usually do this in a separate namespace...
        // Prepare the Python code to execute
        string pythonCode = @"
        import json

        obj = { 'Name': 'Alice', 'Age': 30 }
        json_str = json.dumps(obj)
        print(json_str)
        ";

        // Create KernelArguments with the code
        var pythonArguments = new KernelArguments
        {
            ["input"] = pythonCode
        };

        // Execute the Python code
        string pythonResult = await plugin.ExecutePythonCode(pythonArguments);

        // Output the result
        Console.WriteLine("Python Execution Result:");
        Console.WriteLine(pythonResult);


    }
}
