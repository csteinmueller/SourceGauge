using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SourceGauge.Cli;

internal static class MetricResultJson
{
    internal const string SchemaVersion = "1";

    private static readonly UTF8Encoding _strictUtf8 = new(false, true);

    private static readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    internal static byte[] Serialize(MetricResultDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ValidateDocument(document);

        MetricRecord[] metrics = document.Metrics.ToArray();
        SortEntry[] entries = new SortEntry[metrics.Length];
        for (int index = 0; index < metrics.Length; index++)
        {
            entries[index] = new SortEntry(metrics[index], CreateSortKey(metrics[index]));
        }

        Array.Sort(entries, SortEntryComparer.Instance);
        MetricResultDocument orderedDocument = document with
        {
            Metrics = entries.Select(entry => entry.Metric).ToArray()
        };

        return JsonSerializer.SerializeToUtf8Bytes(orderedDocument, _options);
    }

    private static void ValidateDocument(MetricResultDocument document)
    {
        if (document.SchemaVersion != SchemaVersion)
        {
            throw new ArgumentException($"Unsupported schema version '{document.SchemaVersion}'.", nameof(document));
        }

        ValidateText(document.Tool.Name, nameof(document.Tool.Name));
        ValidateText(document.Tool.Version, nameof(document.Tool.Version));
        if (document.Run.Input.Kind is not ("project" or "solution"))
        {
            throw new ArgumentException("Input kind must be 'project' or 'solution'.", nameof(document));
        }

        ValidateRelativePath(document.Run.Input.Path, nameof(document.Run.Input.Path));

        HashSet<MetricIdentity> identities = new();
        foreach (MetricRecord metric in document.Metrics)
        {
            ValidateText(metric.Id, nameof(metric.Id));
            ValidateText(metric.Scope, nameof(metric.Scope));
            ValidateText(metric.Entity.Kind, nameof(metric.Entity.Kind));
            ValidateRelativePath(metric.Entity.ProjectId, nameof(metric.Entity.ProjectId));
            ValidateText(metric.Entity.Id, nameof(metric.Entity.Id));
            ValidateText(metric.Unit, nameof(metric.Unit));
            if (metric.Entity.DisplayName is not null)
            {
                ValidateText(metric.Entity.DisplayName, nameof(metric.Entity.DisplayName));
            }

            if (metric.Source is not null)
            {
                ValidateRelativePath(metric.Source.Path, nameof(metric.Source.Path));
            }

            MetricIdentity identity = new(
                metric.Id,
                metric.Scope,
                metric.Entity.ProjectId,
                metric.Entity.Kind,
                metric.Entity.Id);
            if (!identities.Add(identity))
            {
                throw new ArgumentException("Metric records must have unique metric, scope, project, and entity identities.", nameof(document));
            }
        }
    }

    private static void ValidateText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('\0'))
        {
            throw new ArgumentException("Contract text values must be non-empty and cannot contain NUL.", parameterName);
        }

        _ = _strictUtf8.GetByteCount(value);
    }

    private static void ValidateRelativePath(string path, string parameterName)
    {
        ValidateText(path, parameterName);
        if ((path.Length > 0 && path[0] == '/')
            || path.Contains('\\')
            || path.Any(char.IsControl)
            || (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':'))
        {
            throw new ArgumentException("Contract paths must be normalized relative paths using '/'.", parameterName);
        }

        string[] segments = path.Split('/');
        bool foundNonParentSegment = false;
        for (int index = 0; index < segments.Length; index++)
        {
            string segment = segments[index];
            if (segment.Length == 0 || segment == "." || (segment == ".." && foundNonParentSegment))
            {
                throw new ArgumentException("Contract paths must be normalized relative paths using '/'.", parameterName);
            }

            if (segment == "..")
            {
                if (index == segments.Length - 1)
                {
                    throw new ArgumentException("Contract paths must identify a file, not a parent directory.", parameterName);
                }

                continue;
            }

            foundNonParentSegment = true;
        }
    }

    private static byte[] CreateSortKey(MetricRecord metric)
    {
        string[] fields =
        [
            metric.Id,
            metric.Scope,
            metric.Entity.ProjectId,
            metric.Entity.Kind,
            metric.Entity.Id
        ];

        int length = fields.Sum(field => _strictUtf8.GetByteCount(field) + 1);
        byte[] key = new byte[length];
        int offset = 0;
        foreach (string field in fields)
        {
            offset += _strictUtf8.GetBytes(field.AsSpan(), key.AsSpan(offset));
            key[offset++] = 0;
        }

        return key;
    }

    private readonly record struct MetricIdentity(
        string Id,
        string Scope,
        string ProjectId,
        string EntityKind,
        string EntityId);

    private sealed record SortEntry(MetricRecord Metric, byte[] Key);

    private sealed class SortEntryComparer : IComparer<SortEntry>
    {
        internal static SortEntryComparer Instance { get; } = new();

        public int Compare(SortEntry? left, SortEntry? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            int sharedLength = Math.Min(left.Key.Length, right.Key.Length);
            for (int index = 0; index < sharedLength; index++)
            {
                int comparison = left.Key[index].CompareTo(right.Key[index]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return left.Key.Length.CompareTo(right.Key.Length);
        }
    }
}
