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
            "Fixture.Assembly",
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
        Assert.Equal("Fixture.Assembly", target.Entity.AssemblyName);
    }

    [Fact]
    public async Task CollectAsync_MethodDeclarations_UsesDocumentedMethodIds()
    {
        using AdhocWorkspace workspace = new();
        string projectPath = Path.Combine(Path.GetTempPath(), "SourceGauge.CSharp.Tests", "MethodIdFixture.csproj");
        ProjectInfo projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            "MethodIdFixture",
            "MethodIdFixture",
            LanguageNames.CSharp,
            filePath: projectPath,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: CSharpParseOptions.Default,
            metadataReferences: GetPlatformReferences());
        Project project = workspace.AddProject(projectInfo);
        string sourcePath = Path.Combine(Path.GetDirectoryName(projectPath)!, "MethodIdFixture.cs");
        project = project.AddDocument(
            "MethodIdFixture.cs",
            SourceText.From("""
                using System.Collections.Generic;

                namespace Identity;

                internal class Examples
                {
                    internal Examples() { }
                    internal void Parameterless() { }
                    internal void Overloaded(int value) { }
                    internal void Overloaded(string value) { }
                    internal void GenericMethod<T>(T value) { }
                    internal void ByReference(ref int value) { }
                }

                internal class GenericType<T>
                {
                    internal void Accept(List<T> values) { }
                }

                internal class Outer<T>
                {
                    internal class Nested<U>
                    {
                        internal void Use(T outer, U inner) { }
                    }
                }
                """),
            filePath: sourcePath).Project;

        var metrics = await FanInMetricCollector.CollectAsync([project], Path.GetDirectoryName(projectPath)!, "project");
        string[] methodIds = metrics.Select(metric => metric.Entity.Id).ToArray();

        Assert.Contains("Identity.Examples.Parameterless", methodIds);
        Assert.Contains("Identity.Examples.Overloaded(System.Int32)", methodIds);
        Assert.Contains("Identity.Examples.Overloaded(System.String)", methodIds);
        Assert.Contains("Identity.Examples.GenericMethod``1(``0)", methodIds);
        Assert.Contains("Identity.Examples.ByReference(System.Int32@)", methodIds);
        Assert.Contains("Identity.GenericType`1.Accept(System.Collections.Generic.List{`0})", methodIds);
        Assert.Contains("Identity.Outer`1.Nested`1.Use(`0,`1)", methodIds);
        Assert.Contains("Identity.Examples.#ctor", methodIds);
        Assert.Equal(methodIds.Length, methodIds.Distinct(StringComparer.Ordinal).Count());
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
