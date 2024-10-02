namespace CodeExecutionPlugin.plugins;

public class DotNetInteractivePluginV01Options
{
    public bool UseDocker { get; set; } = false;
    public string DockerEndpoint { get; set; } = "http://localhost:2375";
    public string DockerImage { get; set; } = "mcr.microsoft.com/dotnet/sdk:8.0";
    public string PythonDockerImage { get; set; } = "python:3.10-alpine";
    public string WorkingDirectory { get; set; } = "/workspace";
}
