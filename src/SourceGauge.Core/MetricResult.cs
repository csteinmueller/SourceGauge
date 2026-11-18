using System.Collections.Generic;

namespace SourceGauge.Core;

internal sealed record MetricResultDocument(
    string SchemaVersion,
    MetricTool Tool,
    MetricRun Run,
    IReadOnlyList<MetricRecord> Metrics);

internal sealed record MetricTool(
    string Name,
    string Version);

internal sealed record MetricRun(
    MetricInput Input,
    string SourceBasePath,
    bool AnalysisComplete,
    IReadOnlyList<MetricProject> Projects);

internal sealed record MetricInput(
    string Kind,
    string Path);

internal sealed record MetricProject(
    string ProjectId,
    string TargetFramework,
    string AssemblyName,
    int SourceDocumentCount,
    int CompilerErrorCount);

internal sealed record MetricRecord(
    string Id,
    string Scope,
    MetricEntity Entity,
    MetricSource? Source,
    decimal Value,
    string Unit);

internal sealed record MetricEntity(
    string Kind,
    string ProjectId,
    string? AssemblyName,
    string Id,
    string? DisplayName);

internal sealed record MetricSource(
    string Path);
