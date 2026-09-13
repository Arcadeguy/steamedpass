using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;

namespace Steamedpass.Core.Scripting;

/// <summary>
/// Runs an ad-hoc PowerShell script and returns its string output.
/// Adapted from UWPHook's ScriptManager (https://github.com/BrianLima/UWPHook),
/// MIT License, Copyright (c) 2016 Brian Lima.
/// </summary>
public static class PowerShellRunner
{
    public static string Run(string scriptText)
    {
        using Runspace runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();

        using var pipeline = runspace.CreatePipeline();
        pipeline.Commands.AddScript(scriptText);
        pipeline.Commands.Add("Out-String");

        Collection<PSObject> results = pipeline.Invoke();

        var stringBuilder = new StringBuilder();
        foreach (PSObject obj in results)
        {
            stringBuilder.AppendLine(obj.ToString());
        }

        return stringBuilder.ToString();
    }
}
