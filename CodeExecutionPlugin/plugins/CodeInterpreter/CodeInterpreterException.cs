using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeExecutionPlugin.plugins;

// Copyright (c) Kevin BEAUGRAND. All rights reserved.

public class CodeInterpreterException : Exception
{
    internal CodeInterpreterException(string message, params string[] warnings)
        : base(message)
    {
        this.Warnings = warnings;
    }

    public string[] Warnings { get; }
}
