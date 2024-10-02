namespace CodeExecutionPlugin.plugins;
public class DotNetInteractivePluginOptions
{
    public bool UseDocker { get; set; } = false;
    public string DockerEndpoint { get; set; } = "unix:///var/run/docker.sock";
    public string DockerImage { get; set; } = "mcr.microsoft.com/dotnet/sdk:7.0";
    public string PythonDockerImage { get; set; } = "python:3.10-alpine";
    public string WorkingDirectory { get; set; } = "/workspace";
    public List<string> AllowedAssemblies { get; set; } = new List<string>();

    // Add Python-specific options
    public string PythonKernelSpecPath { get; set; } = ""; // Path to the Python kernel spec
    public string PythonKernelName { get; set; } = "python";
}
