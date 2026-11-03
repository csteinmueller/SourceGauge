using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SourceGauge.Core;
using SourceGauge.CSharp;
using Xunit;

namespace SourceGauge.CSharp.Tests;

public class FanInMetricCollectorTests
{
    [Fact]
    public async Task CollectAsync_ProjectCountsDistinctCallersAndZeroValues()
    {
        using AdhocWorkspace workspace = new();
        string projectPath = Path.Combine(Path.GetTempPath(), "SourceGauge.CSharp.Tests", "FanInFixture.csproj");
        ProjectInfo projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            "FanInFixture",
            "FanInFixture",
            LanguageNames.CSharp,
            filePath: projectPath,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: CSharpParseOptions.Default,
            metadataReferences: GetPlatformReferences());
        Project project = workspace.AddProject(projectInfo);
        string sourcePath = Path.Combine(Path.GetDirectoryName(projectPath)!, "FanInFixture.cs");
        project = project.AddDocument(
            "FanInFixture.cs",
            SourceText.From("""
                namespace Fixture;

                internal static class FanInFixture
                {
                    internal static void Target() { }
                    internal static void Unused() { }
                    internal static void RepeatedCaller()
                    {
                        Target();
                        Target();
                    }
                    internal static void DistinctCaller() => Target();
                }
                """),
            filePath: sourcePath).Project;

        var metrics = await FanInMetricCollector.CollectAsync([project], Path.GetDirectoryName(projectPath)!, "project");

        MetricRecord target = Assert.Single(metrics, metric => metric.Entity.Id.EndsWith("FanInFixture.Target", StringComparison.Ordinal));
        MetricRecord unused = Assert.Single(metrics, metric => metric.Entity.Id.EndsWith("FanInFixture.Unused", StringComparison.Ordinal));
        Assert.Equal(2m, target.Value);
        Assert.Equal(0m, unused.Value);
    }

    private static MetadataReference[] GetPlatformReferences()
    {
        string trustedAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}
