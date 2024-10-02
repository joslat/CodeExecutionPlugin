using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeExecutionPlugin.plugins;

namespace CodeExecutionPlugin;

public static class DotNetInteractiveTest
{
    public static async Task Execute()
    {
        Console.WriteLine("Hello from DotNetInteractiveTest");

        var options = new DotNetInteractivePluginOptions();
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
        var plugin = new DotNetInteractivePlugin(options, loggerFactory);

        // Prepare the C# code to execute
        string csharpCode = @"
        #r ""nuget: Newtonsoft.Json, 13.0.1""

        using Newtonsoft.Json;

        Console.WriteLine(""Hello, World!"");

        var obj = new { Name = ""Jane Doe"", Age = 25 };
        var json = JsonConvert.SerializeObject(obj);
        Console.WriteLine(json);
        ";

        //string csharpCode = @"
        //Console.WriteLine(""Hello, World!"");
        //";

        // Create KernelArguments with the code
        var csharpArguments = new KernelArguments
        {
            ["input"] = csharpCode
        };

        // Execute the C# code
        string csharpResult = await plugin.ExecuteCSharpCodeAsync(csharpCode);

        // Output the result
        Console.WriteLine("C# Execution Result:");
        Console.WriteLine(csharpResult);

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
        string pythonResult = await plugin.ExecutePythonCodeAsync(pythonCode);

        // Output the result
        Console.WriteLine("Python Execution Result:");
        Console.WriteLine(pythonResult);


    }
}
