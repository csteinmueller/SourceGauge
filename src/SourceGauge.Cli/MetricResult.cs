using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SourceGauge.Cli;

internal sealed record MetricResultDocument(
    [property: JsonPropertyOrder(0)] string SchemaVersion,
    [property: JsonPropertyOrder(1)] MetricTool Tool,
    [property: JsonPropertyOrder(2)] MetricRun Run,
    [property: JsonPropertyOrder(3)] IReadOnlyList<MetricRecord> Metrics);

internal sealed record MetricTool(
    [property: JsonPropertyOrder(0)] string Name,
    [property: JsonPropertyOrder(1)] string Version);

internal sealed record MetricRun(
    [property: JsonPropertyOrder(0)] MetricInput Input);

internal sealed record MetricInput(
    [property: JsonPropertyOrder(0)] string Kind,
    [property: JsonPropertyOrder(1)] string Path);

internal sealed record MetricRecord(
    [property: JsonPropertyOrder(0)] string Id,
    [property: JsonPropertyOrder(1)] string Scope,
    [property: JsonPropertyOrder(2)] MetricEntity Entity,
    [property: JsonPropertyOrder(3)] MetricSource? Source,
    [property: JsonPropertyOrder(4)] decimal Value,
    [property: JsonPropertyOrder(5)] string Unit);

internal sealed record MetricEntity(
    [property: JsonPropertyOrder(0)] string Kind,
    [property: JsonPropertyOrder(1)] string ProjectId,
    [property: JsonPropertyOrder(2)] string Id,
    [property: JsonPropertyOrder(3)] string? DisplayName);

internal sealed record MetricSource(
    [property: JsonPropertyOrder(0)] string Path);
