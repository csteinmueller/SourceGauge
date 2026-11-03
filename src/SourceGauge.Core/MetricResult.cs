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
    MetricInput Input);

internal sealed record MetricInput(
    string Kind,
    string Path);

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
    string Id,
    string? DisplayName);

internal sealed record MetricSource(
    string Path);
