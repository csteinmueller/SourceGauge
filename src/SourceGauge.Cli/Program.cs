using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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

    private const string _usage = "Usage: SourceGauge [--output <file>] <project.csproj|solution.sln> | --help | --version";

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

        if (!TryParseArguments(args, out string? inputArgument, out string? outputArgument, out string? argumentError))
        {
            await error.WriteLineAsync(argumentError ?? "Error: provide exactly one project or solution path.").ConfigureAwait(false);
            await error.WriteLineAsync(_usage).ConfigureAwait(false);
            return _usageErrorExitCode;
        }

        string inputPath;
        string? outputPath;
        try
        {
            inputPath = Path.GetFullPath(inputArgument!);
            outputPath = outputArgument is null ? null : Path.GetFullPath(outputArgument);
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

        if (outputPath is not null && PathsReferToSameFile(inputPath, outputPath))
        {
            await error.WriteLineAsync("Error: output file must differ from the input project or solution.").ConfigureAwait(false);
            return _usageErrorExitCode;
        }

        string relativeInputPath = Path.GetRelativePath(Environment.CurrentDirectory, inputPath);
        if (Path.IsPathRooted(relativeInputPath))
        {
            await error.WriteLineAsync("Error: input and working directory must be on the same volume for a relative input identity.").ConfigureAwait(false);
            return _usageErrorExitCode;
        }

        relativeInputPath = NormalizeSeparators(relativeInputPath);

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

        LoadedInput? loadedInput;
        try
        {
            RegisterMsBuild();
            loadedInput = await LoadInputAsync(inputPath, isProject, relativeInputPath, error).ConfigureAwait(false);
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

        if (loadedInput is null)
        {
            return _analysisFailureExitCode;
        }

        try
        {
            MetricResultDocument document = new(
                MetricResultJson.SchemaVersion,
                new MetricTool(
                    "SourceGauge",
                    typeof(CliApplication).Assembly.GetName().Version?.ToString(3) ?? "unknown"),
                new MetricRun(new MetricInput(loadedInput.Kind, loadedInput.Path)),
                loadedInput.Metrics);
            byte[] json = MetricResultJson.Serialize(document);

            if (outputPath is null)
            {
                await WriteToStandardOutputAsync(output, json).ConfigureAwait(false);
            }
            else
            {
                await WriteOutputFileAsync(outputPath, json).ConfigureAwait(false);
            }

            return _successExitCode;
        }
        catch (IOException exception)
        {
            await error.WriteLineAsync($"Export failed: {exception.Message}").ConfigureAwait(false);
            return _analysisFailureExitCode;
        }
        catch (UnauthorizedAccessException exception)
        {
            await error.WriteLineAsync($"Export failed: {exception.Message}").ConfigureAwait(false);
            return _analysisFailureExitCode;
        }
        catch (InvalidOperationException exception)
        {
            await error.WriteLineAsync($"Export failed: {exception.Message}").ConfigureAwait(false);
            return _analysisFailureExitCode;
        }
        catch (ArgumentException exception)
        {
            await error.WriteLineAsync($"Export failed: {exception.Message}").ConfigureAwait(false);
            return _analysisFailureExitCode;
        }
        catch (JsonException exception)
        {
            await error.WriteLineAsync($"Export failed: {exception.Message}").ConfigureAwait(false);
            return _analysisFailureExitCode;
        }
        catch (NotSupportedException exception)
        {
            await error.WriteLineAsync($"Export failed: {exception.Message}").ConfigureAwait(false);
            return _analysisFailureExitCode;
        }
    }

    private static async Task<LoadedInput?> LoadInputAsync(
        string inputPath,
        bool isProject,
        string relativeInputPath,
        TextWriter error)
    {
        string inputRoot = Path.GetDirectoryName(inputPath)!;
        if (isProject)
        {
            return await LoadProjectInputAsync(inputPath, relativeInputPath, inputRoot, error).ConfigureAwait(false);
        }

        using MSBuildWorkspace workspace = MSBuildWorkspace.Create();
        ConcurrentQueue<WorkspaceDiagnostic> diagnostics = new();
        workspace.RegisterWorkspaceFailedHandler(eventArgs => diagnostics.Enqueue(eventArgs.Diagnostic));

        Solution solution = await workspace.OpenSolutionAsync(inputPath).ConfigureAwait(false);
        Project[] projects = solution.Projects.ToArray();

        WorkspaceDiagnostic[] reportedDiagnostics = diagnostics.ToArray();
        foreach (WorkspaceDiagnostic diagnostic in reportedDiagnostics)
        {
            await error.WriteLineAsync($"{diagnostic.Kind}: {diagnostic.Message}").ConfigureAwait(false);
        }

        if (reportedDiagnostics.Any(diagnostic => diagnostic.Kind == WorkspaceDiagnosticKind.Failure))
        {
            return null;
        }

        int documentCount = projects.Sum(project => project.Documents.Count());
        await error.WriteLineAsync($"Loaded {projects.Length} project(s) and {documentCount} document(s).").ConfigureAwait(false);
        IReadOnlyList<MetricRecord> metrics = await FanInMetricCollector.CollectAsync(
            projects,
            inputRoot,
            "solution").ConfigureAwait(false);
        return new LoadedInput("solution", relativeInputPath, metrics);
    }

    private static async Task<LoadedInput?> LoadProjectInputAsync(
        string inputPath,
        string relativeInputPath,
        string inputRoot,
        TextWriter error)
    {
        string[] targetFrameworks = await GetTargetFrameworksAsync(inputPath).ConfigureAwait(false);
        if (targetFrameworks.Length <= 1)
        {
            using MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            ConcurrentQueue<WorkspaceDiagnostic> diagnostics = new();
            workspace.RegisterWorkspaceFailedHandler(eventArgs => diagnostics.Enqueue(eventArgs.Diagnostic));
            Project project = await workspace.OpenProjectAsync(inputPath).ConfigureAwait(false);
            WorkspaceDiagnostic[] reportedDiagnostics = diagnostics.ToArray();
            await WriteWorkspaceDiagnosticsAsync(reportedDiagnostics, error).ConfigureAwait(false);
            if (reportedDiagnostics.Any(diagnostic => diagnostic.Kind == WorkspaceDiagnosticKind.Failure))
            {
                return null;
            }

            await error.WriteLineAsync($"Loaded 1 project(s) and {project.Documents.Count()} document(s).").ConfigureAwait(false);
            IReadOnlyList<MetricRecord> metrics = await FanInMetricCollector.CollectAsync([project], inputRoot, "project").ConfigureAwait(false);
            return new LoadedInput("project", relativeInputPath, metrics);
        }

        List<MetricRecord> allMetrics = [];
        int documentCount = 0;
        foreach (string targetFramework in targetFrameworks)
        {
            Dictionary<string, string> properties = new(StringComparer.Ordinal)
            {
                ["TargetFramework"] = targetFramework
            };
            using MSBuildWorkspace workspace = MSBuildWorkspace.Create(properties);
            ConcurrentQueue<WorkspaceDiagnostic> diagnostics = new();
            workspace.RegisterWorkspaceFailedHandler(eventArgs => diagnostics.Enqueue(eventArgs.Diagnostic));
            Project project = await workspace.OpenProjectAsync(inputPath).ConfigureAwait(false);
            WorkspaceDiagnostic[] reportedDiagnostics = diagnostics.ToArray();
            await WriteWorkspaceDiagnosticsAsync(reportedDiagnostics, error).ConfigureAwait(false);
            if (reportedDiagnostics.Any(diagnostic => diagnostic.Kind == WorkspaceDiagnosticKind.Failure))
            {
                return null;
            }

            documentCount += project.Documents.Count();
            IReadOnlyList<MetricRecord> metrics = await FanInMetricCollector.CollectAsync([project], inputRoot, "project").ConfigureAwait(false);
            allMetrics.AddRange(metrics.Select(metric => metric with
            {
                Entity = metric.Entity with { ProjectId = $"{metric.Entity.ProjectId}#{targetFramework}" }
            }));
        }

        await error.WriteLineAsync($"Loaded 1 project across {targetFrameworks.Length} target frameworks and {documentCount} document(s).").ConfigureAwait(false);
        return new LoadedInput("project", relativeInputPath, allMetrics);
    }

    private static async Task<string[]> GetTargetFrameworksAsync(string projectPath)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("-nologo");
        startInfo.ArgumentList.Add("-getProperty:TargetFrameworks,TargetFramework");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet MSBuild to inspect target frameworks.");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        string output = await standardOutput.ConfigureAwait(false);
        string errorOutput = await standardError.ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failure: Could not inspect target frameworks: {errorOutput}");
        }

        using JsonDocument propertiesDocument = JsonDocument.Parse(output);
        JsonElement properties = propertiesDocument.RootElement.GetProperty("Properties");
        string frameworks = properties.GetProperty("TargetFrameworks").GetString() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(frameworks))
        {
            return frameworks.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        return [];
    }

    private static async Task WriteWorkspaceDiagnosticsAsync(WorkspaceDiagnostic[] diagnostics, TextWriter error)
    {
        foreach (WorkspaceDiagnostic diagnostic in diagnostics)
        {
            await error.WriteLineAsync($"{diagnostic.Kind}: {diagnostic.Message}").ConfigureAwait(false);
        }
    }

    private static bool TryParseArguments(
        string[] args,
        out string? inputPath,
        out string? outputPath,
        out string? errorMessage)
    {
        inputPath = null;
        outputPath = null;
        errorMessage = null;

        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];
            if (argument == "--output")
            {
                if (outputPath is not null || index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
                {
                    errorMessage = "Error: --output requires one file path and may appear only once.";
                    return false;
                }

                outputPath = args[++index];
                continue;
            }

            if (argument.StartsWith("--", StringComparison.Ordinal))
            {
                errorMessage = $"Error: unknown option: {argument}";
                return false;
            }

            if (inputPath is not null)
            {
                errorMessage = "Error: provide exactly one project or solution path.";
                return false;
            }

            inputPath = argument;
        }

        if (inputPath is null)
        {
            errorMessage = "Error: provide exactly one project or solution path.";
            return false;
        }

        return true;
    }

    private static async Task WriteToStandardOutputAsync(TextWriter output, byte[] json)
    {
        string document = Encoding.UTF8.GetString(json) + "\n";
        await output.WriteAsync(document).ConfigureAwait(false);
        await output.FlushAsync().ConfigureAwait(false);
    }

    private static async Task WriteOutputFileAsync(string outputPath, byte[] json)
    {
        string outputDirectory = Path.GetDirectoryName(outputPath)!;
        string temporaryPath = Path.Combine(outputDirectory, $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
        byte[] newline = [0x0A];

        try
        {
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous))
            {
                await stream.WriteAsync(json).ConfigureAwait(false);
                await stream.WriteAsync(newline).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }

            File.Move(temporaryPath, outputPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string NormalizeSeparators(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private static StringComparison GetPathComparison()
    {
        return OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    private static bool PathsReferToSameFile(string inputPath, string outputPath)
    {
        if (string.Equals(inputPath, outputPath, GetPathComparison()))
        {
            return true;
        }

        string resolvedInputPath = ResolveExistingFilePath(inputPath);
        string resolvedOutputPath = ResolveExistingFilePath(outputPath);
        return string.Equals(resolvedInputPath, resolvedOutputPath, GetPathComparison());
    }

    private static string ResolveExistingFilePath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string? root = Path.GetPathRoot(fullPath);
        if (root is null)
        {
            return fullPath;
        }

        string resolvedPath = root;
        string[] segments = fullPath[root.Length..].Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        foreach (string segment in segments)
        {
            string candidate = Path.Combine(resolvedPath, segment);
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                resolvedPath = candidate;
                continue;
            }

            FileSystemInfo fileSystemInfo = Directory.Exists(candidate)
                ? new DirectoryInfo(candidate)
                : new FileInfo(candidate);
            FileSystemInfo? target = fileSystemInfo.ResolveLinkTarget(returnFinalTarget: true);
            resolvedPath = Path.GetFullPath(target?.FullName ?? candidate);
        }

        return resolvedPath;
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

    private sealed record LoadedInput(string Kind, string Path, IReadOnlyList<MetricRecord> Metrics);
}
