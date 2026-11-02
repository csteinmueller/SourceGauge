using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SourceGauge.Cli.Tests;

public class CliApplicationTests
{
    [Fact]
    public async Task RunAsync_HelpRequested_WritesUsageToStandardOutput()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = await CliApplication.RunAsync(["--help"], output, error);

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage:", output.ToString(), StringComparison.Ordinal);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public async Task RunAsync_VersionRequested_WritesVersionToStandardOutput()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = await CliApplication.RunAsync(["--version"], output, error);

        Assert.Equal(0, exitCode);
        Assert.Equal("0.1.0" + Environment.NewLine, output.ToString());
        Assert.Empty(error.ToString());
    }

    [Fact]
    public async Task RunAsync_NoArguments_ReturnsUsageError()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = await CliApplication.RunAsync([], output, error);

        Assert.Equal(2, exitCode);
        Assert.Contains("Usage:", error.ToString(), StringComparison.Ordinal);
        Assert.Empty(output.ToString());
    }

    [Fact]
    public async Task RunAsync_MultipleInputPaths_ReturnsUsageError()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = await CliApplication.RunAsync(["first.csproj", "second.csproj"], output, error);

        Assert.Equal(2, exitCode);
        Assert.Contains("Usage:", error.ToString(), StringComparison.Ordinal);
        Assert.Empty(output.ToString());
    }

    [Fact]
    public async Task RunAsync_UnsupportedInput_ReturnsUsageError()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = await CliApplication.RunAsync(["source.cs"], output, error);

        Assert.Equal(2, exitCode);
        Assert.Contains(".csproj", error.ToString(), StringComparison.Ordinal);
        Assert.Empty(output.ToString());
    }

    [Fact]
    public async Task RunAsync_ProjectAndSolution_WritesEmptyJsonAndKeepsMessagesOnStandardError()
    {
        string fixtureDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
        string projectPath = Path.Combine(fixtureDirectory, "FixtureProject.csproj");
        string solutionPath = Path.Combine(fixtureDirectory, "FixtureSolution.sln");

        foreach (string inputPath in new[] { projectPath, solutionPath })
        {
            using StringWriter output = new();
            using StringWriter error = new();

            int exitCode = await CliApplication.RunAsync([inputPath], output, error);

            Assert.Equal(0, exitCode);
            using JsonDocument document = JsonDocument.Parse(output.ToString());
            Assert.Equal("1", document.RootElement.GetProperty("schemaVersion").GetString());
            Assert.Empty(document.RootElement.GetProperty("metrics").EnumerateArray());
            Assert.Contains("Loaded", error.ToString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task RunAsync_OutputPath_WritesJsonFileAndKeepsStandardOutputEmpty()
    {
        string fixtureDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
        string projectPath = Path.Combine(fixtureDirectory, "FixtureProject.csproj");
        string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"SourceGauge.Cli.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        string outputPath = Path.Combine(temporaryDirectory, "metrics.json");
        await File.WriteAllTextAsync(outputPath, "old output");

        try
        {
            using StringWriter output = new();
            using StringWriter error = new();

            int exitCode = await CliApplication.RunAsync(["--output", outputPath, projectPath], output, error);

            Assert.Equal(0, exitCode);
            Assert.Empty(output.ToString());
            string json = await File.ReadAllTextAsync(outputPath);
            Assert.EndsWith("\n", json, StringComparison.Ordinal);
            using JsonDocument document = JsonDocument.Parse(json);
            Assert.Equal("project", document.RootElement.GetProperty("run").GetProperty("input").GetProperty("kind").GetString());
            Assert.Contains("Loaded", error.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_OutputWriterFails_ReturnsExportFailure()
    {
        string fixtureDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
        string projectPath = Path.Combine(fixtureDirectory, "FixtureProject.csproj");
        using ThrowingTextWriter output = new();
        using StringWriter error = new();

        int exitCode = await CliApplication.RunAsync([projectPath], output, error);

        Assert.Equal(1, exitCode);
        Assert.Contains("Export failed:", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("Loaded", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_OutputDirectoryMissing_ReturnsExportFailure()
    {
        string fixtureDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
        string projectPath = Path.Combine(fixtureDirectory, "FixtureProject.csproj");
        string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"SourceGauge.Cli.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        string outputPath = Path.Combine(temporaryDirectory, "missing", "metrics.json");

        try
        {
            using StringWriter output = new();
            using StringWriter error = new();

            int exitCode = await CliApplication.RunAsync(["--output", outputPath, projectPath], output, error);

            Assert.Equal(1, exitCode);
            Assert.Contains("Export failed:", error.ToString(), StringComparison.Ordinal);
            Assert.Empty(output.ToString());
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_OutputOptionWithoutPath_ReturnsUsageError()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = await CliApplication.RunAsync(["--output"], output, error);

        Assert.Equal(2, exitCode);
        Assert.Contains("--output requires", error.ToString(), StringComparison.Ordinal);
        Assert.Empty(output.ToString());
    }

    [Fact]
    public async Task RunAsync_InputOnDifferentWindowsVolume_ReturnsUsageErrorBeforeAnalysis()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string currentRoot = Path.GetPathRoot(Environment.CurrentDirectory)!;
        string otherRoot = string.Equals(currentRoot, "C:\\", StringComparison.OrdinalIgnoreCase) ? "D:\\" : "C:\\";
        string inputPath = Path.Combine(otherRoot, "missing", "project.csproj");
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = await CliApplication.RunAsync([inputPath], output, error);

        Assert.Equal(2, exitCode);
        Assert.Contains("same volume", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(output.ToString());
    }

    [Fact]
    public async Task RunAsync_SymlinkInputAndTargetOutput_ReturnsUsageErrorWithoutOverwritingInput()
    {
        string fixtureDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
        string sourceProjectPath = Path.Combine(fixtureDirectory, "FixtureProject.csproj");
        string temporaryDirectory = Path.Combine(Environment.CurrentDirectory, $"SourceGauge.Cli.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        string targetProjectPath = Path.Combine(temporaryDirectory, "TargetProject.csproj");
        string symlinkProjectPath = Path.Combine(temporaryDirectory, "LinkedProject.csproj");
        File.Copy(sourceProjectPath, targetProjectPath);

        try
        {
            File.CreateSymbolicLink(symlinkProjectPath, targetProjectPath);
        }
        catch (UnauthorizedAccessException)
        {
            Directory.Delete(temporaryDirectory, recursive: true);
            return;
        }
        catch (PlatformNotSupportedException)
        {
            Directory.Delete(temporaryDirectory, recursive: true);
            return;
        }
        catch (IOException)
        {
            Directory.Delete(temporaryDirectory, recursive: true);
            return;
        }

        try
        {
            string originalContent = await File.ReadAllTextAsync(targetProjectPath);
            using StringWriter output = new();
            using StringWriter error = new();

            int exitCode = await CliApplication.RunAsync(["--output", targetProjectPath, symlinkProjectPath], output, error);

            Assert.Equal(2, exitCode);
            Assert.Contains("output file must differ", error.ToString(), StringComparison.Ordinal);
            Assert.Equal(originalContent, await File.ReadAllTextAsync(targetProjectPath));
            Assert.Empty(output.ToString());
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_CaseAliasOutputOnCaseInsensitiveFileSystem_ReturnsUsageError()
    {
        string fixtureDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
        string sourceProjectPath = Path.Combine(fixtureDirectory, "FixtureProject.csproj");
        string temporaryDirectory = Path.Combine(Environment.CurrentDirectory, $"SourceGauge.Cli.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        string inputPath = Path.Combine(temporaryDirectory, "Project.csproj");
        string outputPath = Path.Combine(temporaryDirectory, "project.csproj");
        File.Copy(sourceProjectPath, inputPath);

        try
        {
            if (!File.Exists(outputPath))
            {
                return;
            }

            string originalContent = await File.ReadAllTextAsync(inputPath);
            using StringWriter output = new();
            using StringWriter error = new();

            int exitCode = await CliApplication.RunAsync(["--output", outputPath, inputPath], output, error);

            Assert.Equal(2, exitCode);
            Assert.Contains("output file must differ", error.ToString(), StringComparison.Ordinal);
            Assert.Equal(originalContent, await File.ReadAllTextAsync(inputPath));
            Assert.Empty(output.ToString());
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_ParentDirectorySymlinkAndRealPathOutput_ReturnsUsageError()
    {
        string fixtureDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
        string sourceProjectPath = Path.Combine(fixtureDirectory, "FixtureProject.csproj");
        string temporaryDirectory = Path.Combine(Environment.CurrentDirectory, $"SourceGauge.Cli.Tests.{Guid.NewGuid():N}");
        string targetDirectory = Path.Combine(temporaryDirectory, "target");
        Directory.CreateDirectory(targetDirectory);
        string inputPath = Path.Combine(targetDirectory, "Project.csproj");
        string originalContent = await File.ReadAllTextAsync(sourceProjectPath);
        await File.WriteAllTextAsync(inputPath, originalContent);
        string linkedDirectory = Path.Combine(temporaryDirectory, "linked");

        try
        {
            Directory.CreateSymbolicLink(linkedDirectory, targetDirectory);
        }
        catch (UnauthorizedAccessException)
        {
            Directory.Delete(temporaryDirectory, recursive: true);
            return;
        }
        catch (PlatformNotSupportedException)
        {
            Directory.Delete(temporaryDirectory, recursive: true);
            return;
        }
        catch (IOException)
        {
            Directory.Delete(temporaryDirectory, recursive: true);
            return;
        }

        try
        {
            string aliasedOutputPath = Path.Combine(linkedDirectory, "Project.csproj");
            using StringWriter output = new();
            using StringWriter error = new();

            int exitCode = await CliApplication.RunAsync(["--output", aliasedOutputPath, inputPath], output, error);

            Assert.Equal(2, exitCode);
            Assert.Contains("output file must differ", error.ToString(), StringComparison.Ordinal);
            Assert.Equal(originalContent, await File.ReadAllTextAsync(inputPath));
            Assert.Empty(output.ToString());
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_InvalidProjectFile_ReturnsAnalysisFailure()
    {
        string temporaryDirectory = Path.Combine(Environment.CurrentDirectory, $"SourceGauge.Cli.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        string projectPath = Path.Combine(temporaryDirectory, "InvalidProject.csproj");
        await File.WriteAllTextAsync(projectPath, "<Project><PropertyGroup>");

        try
        {
            using StringWriter output = new();
            using StringWriter error = new();

            int exitCode = await CliApplication.RunAsync([projectPath], output, error);

            Assert.Equal(1, exitCode);
            Assert.Contains("Failure:", error.ToString(), StringComparison.Ordinal);
            Assert.Empty(output.ToString());
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private sealed class ThrowingTextWriter : StringWriter
    {
        public override Task WriteAsync(string? value)
        {
            return Task.FromException(new IOException("Test output failure."));
        }
    }
}
