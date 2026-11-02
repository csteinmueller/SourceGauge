using System;
using System.IO;
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
    public async Task RunAsync_ProjectAndSolution_LoadsInputAndKeepsStandardOutputEmpty()
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
            Assert.Empty(output.ToString());
            Assert.Contains("Loaded", error.ToString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task RunAsync_InvalidProjectFile_ReturnsAnalysisFailure()
    {
        string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"SourceGauge.Cli.Tests.{Guid.NewGuid():N}");
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
}
