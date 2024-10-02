using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeExecutionPlugin.plugins;

// Copyright (c) Kevin BEAUGRAND. All rights reserved.

public class CodeInterpretionPluginOptions
{
    public string DockerEndpoint { get; set; } = string.Empty;

    public string DockerImage { get; set; } = "python:3-alpine";

    public string OutputFilePath { get; set; } = ".";

    public string GoogleSearchAPIKey { get; set; } = string.Empty;

    public string GoogleSearchEngineId { get; set; } = string.Empty;
}