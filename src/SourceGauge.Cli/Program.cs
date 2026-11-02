using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace SourceGauge.Cli;

internal static class Program
{
    public static Task<int> Main(string[] args)
    {
        return CliApplication.RunAsync(args, Console.Out, Console.Error);
    }
}

internal static class CliApplication
{
    private const int _successExitCode = 0;
    private const int _analysisFailureExitCode = 1;
    private const int _usageErrorExitCode = 2;

    private const string _usage = "Usage: SourceGauge <project.csproj|solution.sln> | --help | --version";

    private static readonly object _msBuildRegistrationLock = new();

    internal static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 1 && args[0] == "--help")
        {
            await output.WriteLineAsync(_usage).ConfigureAwait(false);
            return _successExitCode;
        }

        if (args.Length == 1 && args[0] == "--version")
        {
            string version = typeof(CliApplication).Assembly.GetName().Version?.ToString(3) ?? "unknown";
            await output.WriteLineAsync(version).ConfigureAwait(false);
            return _successExitCode;
        }

        if (args.Length != 1)
        {
            await error.WriteLineAsync("Error: provide exactly one project or solution path.").ConfigureAwait(false);
            await error.WriteLineAsync(_usage).ConfigureAwait(false);
            return _usageErrorExitCode;
        }

        string inputPath;
        try
        {
            inputPath = Path.GetFullPath(args[0]);
        }
        catch (ArgumentException exception)
        {
            await error.WriteLineAsync($"Error: invalid input path: {exception.Message}").ConfigureAwait(false);
            return _usageErrorExitCode;
        }
        catch (NotSupportedException exception)
        {
            await error.WriteLineAsync($"Error: invalid input path: {exception.Message}").ConfigureAwait(false);
            return _usageErrorExitCode;
        }

        string extension = Path.GetExtension(inputPath);
        bool isProject = extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase);
        bool isSolution = extension.Equals(".sln", StringComparison.OrdinalIgnoreCase);
        if (!isProject && !isSolution)
        {
            await error.WriteLineAsync("Error: input must be a C# project (.csproj) or solution (.sln).").ConfigureAwait(false);
            return _usageErrorExitCode;
        }

        if (!File.Exists(inputPath))
        {
            await error.WriteLineAsync($"Error: input file not found: {inputPath}").ConfigureAwait(false);
            return _usageErrorExitCode;
        }

        try
        {
            RegisterMsBuild();
            return await LoadInputAsync(inputPath, isProject, error).ConfigureAwait(false);
        }
        catch (IOException exception)
        {
            await error.WriteLineAsync($"Analysis failed: {exception.Message}").ConfigureAwait(false);
            return _analysisFailureExitCode;
        }
        catch (UnauthorizedAccessException exception)
        {
            await error.WriteLineAsync($"Analysis failed: {exception.Message}").ConfigureAwait(false);
            return _analysisFailureExitCode;
        }
        catch (InvalidOperationException exception)
        {
            await error.WriteLineAsync($"Analysis failed: {exception.Message}").ConfigureAwait(false);
            return _analysisFailureExitCode;
        }
        catch (ArgumentException exception)
        {
            await error.WriteLineAsync($"Analysis failed: {exception.Message}").ConfigureAwait(false);
            return _analysisFailureExitCode;
        }
    }

    private static async Task<int> LoadInputAsync(string inputPath, bool isProject, TextWriter error)
    {
        using MSBuildWorkspace workspace = MSBuildWorkspace.Create();
        ConcurrentQueue<WorkspaceDiagnostic> diagnostics = new();
        workspace.RegisterWorkspaceFailedHandler(eventArgs => diagnostics.Enqueue(eventArgs.Diagnostic));

        int projectCount;
        int documentCount;
        if (isProject)
        {
            Project project = await workspace.OpenProjectAsync(inputPath).ConfigureAwait(false);
            projectCount = 1;
            documentCount = project.Documents.Count();
        }
        else
        {
            Solution solution = await workspace.OpenSolutionAsync(inputPath).ConfigureAwait(false);
            projectCount = solution.Projects.Count();
            documentCount = solution.Projects.Sum(project => project.Documents.Count());
        }

        WorkspaceDiagnostic[] reportedDiagnostics = diagnostics.ToArray();
        foreach (WorkspaceDiagnostic diagnostic in reportedDiagnostics)
        {
            await error.WriteLineAsync($"{diagnostic.Kind}: {diagnostic.Message}").ConfigureAwait(false);
        }

        if (reportedDiagnostics.Any(diagnostic => diagnostic.Kind == WorkspaceDiagnosticKind.Failure))
        {
            return _analysisFailureExitCode;
        }

        await error.WriteLineAsync($"Loaded {projectCount} project(s) and {documentCount} document(s).").ConfigureAwait(false);
        return _successExitCode;
    }

    private static void RegisterMsBuild()
    {
        lock (_msBuildRegistrationLock)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }
        }
    }
}
