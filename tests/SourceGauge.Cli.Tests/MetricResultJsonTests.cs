using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using SourceGauge.Core;
using Xunit;

namespace SourceGauge.Cli.Tests;

public class MetricResultJsonTests
{
    [Fact]
    public void Serialize_RecordsInDifferentOrder_ReturnsUtf8OrderedRecords()
    {
        MetricResultDocument document = CreateDocument(
        [
            CreateMetric("z.metric", "project.csproj", "Z.Run()", 2),
            CreateMetric("a.metric", "project.csproj", "A.Run()", 1),
            CreateMetric("a.metric", "\uE000.csproj", "Unicode.Run()", 3),
            CreateMetric("a.metric", "\U00010000.csproj", "Unicode.Run()", 4)
        ]);

        byte[] json = MetricResultJson.Serialize(document);
        byte[] reversedJson = MetricResultJson.Serialize(document with
        {
            Metrics = document.Metrics.Reverse().ToArray()
        });
        using JsonDocument parsed = JsonDocument.Parse(json);
        string[] identifiers = parsed.RootElement
            .GetProperty("metrics")
            .EnumerateArray()
            .Select(metric => metric.GetProperty("entity").GetProperty("projectId").GetString()!)
            .ToArray();

        Assert.Equal(["project.csproj", "\uE000.csproj", "\U00010000.csproj", "project.csproj"], identifiers);
        Assert.Equal(json, reversedJson);
    }

    [Fact]
    public void Serialize_EmptyResultsAndEscapedText_ProducesValidJson()
    {
        MetricResultDocument emptyDocument = CreateDocument([]);
        using JsonDocument emptyJson = JsonDocument.Parse(MetricResultJson.Serialize(emptyDocument));
        Assert.Empty(emptyJson.RootElement.GetProperty("metrics").EnumerateArray());

        MetricRecord metric = CreateMetric("sample.metric", "project.csproj", "Run(\"quoted\")", 1);
        MetricResultDocument document = CreateDocument([metric]);
        using JsonDocument parsed = JsonDocument.Parse(MetricResultJson.Serialize(document));
        string? entityId = parsed.RootElement
            .GetProperty("metrics")[0]
            .GetProperty("entity")
            .GetProperty("id")
            .GetString();

        Assert.Equal("Run(\"quoted\")", entityId);
    }

    [Fact]
    public void Serialize_DuplicateIdentity_ThrowsArgumentException()
    {
        MetricRecord metric = CreateMetric("sample.metric", "project.csproj", "Run()", 1);
        MetricResultDocument document = CreateDocument([metric, metric]);

        Assert.Throws<ArgumentException>(() => MetricResultJson.Serialize(document));
    }

    [Theory]
    [InlineData("/absolute/project.csproj")]
    [InlineData("C:/absolute/project.csproj")]
    [InlineData("C:relative/project.csproj")]
    [InlineData("src//project.csproj")]
    [InlineData("src/./project.csproj")]
    [InlineData("src/sub/../project.csproj")]
    [InlineData("src\\project.csproj")]
    [InlineData("src\n/../project.csproj")]
    [InlineData("   ")]
    [InlineData("dir/..\n")]
    [InlineData("dir/\u0085file.csproj")]
    public void Serialize_NonNormalizedInputPath_ThrowsArgumentException(string path)
    {
        MetricResultDocument document = CreateDocument([]) with
        {
            Run = new MetricRun(new MetricInput("project", path))
        };

        Assert.Throws<ArgumentException>(() => MetricResultJson.Serialize(document));

        string schemaPath = FindRepositoryFile("docs", "schema", "metric-results-v1.schema.json");
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllText(schemaPath));
        string pathPattern = schema.RootElement
            .GetProperty("properties")
            .GetProperty("run")
            .GetProperty("properties")
            .GetProperty("input")
            .GetProperty("properties")
            .GetProperty("path")
            .GetProperty("pattern")
            .GetString()!;

        Assert.False(Regex.IsMatch(path, pathPattern, RegexOptions.CultureInvariant));
    }

    [Fact]
    public void Serialize_LeadingParentPath_PreservesRelativeLinkedSourcePath()
    {
        MetricResultDocument document = CreateDocument(
        [CreateMetric("sourcegauge.example.count", "../Shared/Shared.csproj", "Shared.Run()", 1) with
        {
            Source = new MetricSource("../Shared/Shared.cs")
        }]) with
        {
            Run = new MetricRun(new MetricInput("solution", "../repo/App.sln"))
        };

        byte[] json = MetricResultJson.Serialize(document);
        string schemaPath = FindRepositoryFile("docs", "schema", "metric-results-v1.schema.json");
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllText(schemaPath));
        using JsonDocument parsed = JsonDocument.Parse(json);

        Assert.NotEmpty(json);
        AssertConformsToSchema(schema.RootElement, parsed.RootElement, schema.RootElement);
    }

    [Fact]
    public void Serialize_LeadingWhitespacePath_ConformsToSchema()
    {
        MetricResultDocument document = CreateDocument([]) with
        {
            Run = new MetricRun(new MetricInput("project", " project.csproj"))
        };
        string schemaPath = FindRepositoryFile("docs", "schema", "metric-results-v1.schema.json");
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllText(schemaPath));
        using JsonDocument parsed = JsonDocument.Parse(MetricResultJson.Serialize(document));

        AssertConformsToSchema(schema.RootElement, parsed.RootElement, schema.RootElement);
    }

    [Fact]
    public void PublishedSchema_ValidatesSampleAndSerializedResult()
    {
        string schemaPath = FindRepositoryFile("docs", "schema", "metric-results-v1.schema.json");
        string samplePath = FindRepositoryFile("docs", "samples", "metric-results-v1.json");
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllText(schemaPath));
        using JsonDocument sample = JsonDocument.Parse(File.ReadAllText(samplePath));
        using JsonDocument serialized = JsonDocument.Parse(MetricResultJson.Serialize(CreateDocument(
        [CreateMetric("sourcegauge.example.count", "App.csproj", "N.Example`1.Run``1(System.Int32)", 3)])));

        AssertConformsToSchema(schema.RootElement, sample.RootElement, schema.RootElement);
        AssertConformsToSchema(schema.RootElement, serialized.RootElement, schema.RootElement);
    }

    [Theory]
    [InlineData("1e100")]
    [InlineData("0.00000000000000000000000000001")]
    [InlineData("79228162514264337593543950336")]
    [InlineData("9.9999999999999999999999999999")]
    public void PublishedSchema_RejectsValuesOutsideDecimalRangeOrScale(string rawValue)
    {
        string schemaPath = FindRepositoryFile("docs", "schema", "metric-results-v1.schema.json");
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllText(schemaPath));
        using JsonDocument value = JsonDocument.Parse(rawValue);
        JsonElement valueSchema = schema.RootElement
            .GetProperty("$defs")
            .GetProperty("metric")
            .GetProperty("properties")
            .GetProperty("value");

        Assert.False(IsDecimalValueAllowed(valueSchema, value.RootElement));
    }

    private static string FindRepositoryFile(params string[] pathParts)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine([directory.FullName, .. pathParts]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{Path.Combine(pathParts)}'.");
    }

    private static void AssertConformsToSchema(JsonElement schema, JsonElement value, JsonElement rootSchema)
    {
        if (schema.TryGetProperty("$ref", out JsonElement reference))
        {
            string definitionName = reference.GetString()!.Split('/').Last();
            AssertConformsToSchema(rootSchema.GetProperty("$defs").GetProperty(definitionName), value, rootSchema);
            return;
        }

        if (schema.TryGetProperty("const", out JsonElement constant))
        {
            Assert.Equal(constant.GetString(), value.GetString());
        }

        if (schema.TryGetProperty("enum", out JsonElement allowedValues))
        {
            Assert.Contains(value.GetString(), allowedValues.EnumerateArray().Select(item => item.GetString()));
        }

        if (schema.TryGetProperty("pattern", out JsonElement pattern))
        {
            Assert.Matches(new Regex(pattern.GetString()!, RegexOptions.CultureInvariant), value.GetString()!);
        }

        if (!schema.TryGetProperty("type", out JsonElement type))
        {
            return;
        }

        switch (type.GetString())
        {
            case "object":
                Assert.Equal(JsonValueKind.Object, value.ValueKind);
                if (schema.TryGetProperty("required", out JsonElement required))
                {
                    foreach (JsonElement property in required.EnumerateArray())
                    {
                        Assert.True(value.TryGetProperty(property.GetString()!, out _));
                    }
                }

                if (schema.TryGetProperty("properties", out JsonElement properties))
                {
                    foreach (JsonProperty property in properties.EnumerateObject())
                    {
                        if (value.TryGetProperty(property.Name, out JsonElement propertyValue))
                        {
                            AssertConformsToSchema(property.Value, propertyValue, rootSchema);
                        }
                    }
                }

                break;
            case "array":
                Assert.Equal(JsonValueKind.Array, value.ValueKind);
                if (schema.TryGetProperty("items", out JsonElement items))
                {
                    foreach (JsonElement item in value.EnumerateArray())
                    {
                        AssertConformsToSchema(items, item, rootSchema);
                    }
                }

                break;
            case "string":
                Assert.Equal(JsonValueKind.String, value.ValueKind);
                if (schema.TryGetProperty("minLength", out JsonElement minimumLength))
                {
                    Assert.True(value.GetString()!.Length >= minimumLength.GetInt32());
                }

                break;
            case "number":
                Assert.True(IsDecimalValueAllowed(schema, value));
                break;
            default:
                throw new InvalidOperationException($"Unsupported schema type '{type.GetString()}'.");
        }
    }

    private static bool IsDecimalValueAllowed(JsonElement schema, JsonElement value)
    {
        if (schema.TryGetProperty("anyOf", out JsonElement alternatives))
        {
            return alternatives.EnumerateArray().Any(alternative => IsDecimalValueAllowed(alternative, value));
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out decimal decimalValue))
        {
            return false;
        }

        decimal minimum = schema.GetProperty("minimum").GetDecimal();
        decimal maximum = schema.GetProperty("maximum").GetDecimal();
        if (decimalValue < minimum || decimalValue > maximum)
        {
            return false;
        }

        string rawValue = value.GetRawText();
        int exponentPosition = rawValue.IndexOfAny(['e', 'E']);
        string mantissa = exponentPosition < 0 ? rawValue : rawValue[..exponentPosition];
        int exponent = 0;
        if (exponentPosition >= 0
            && !int.TryParse(rawValue[(exponentPosition + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent))
        {
            return decimalValue == 0;
        }

        int decimalPosition = mantissa.IndexOf('.');
        int fractionalDigits = decimalPosition < 0 ? 0 : mantissa.Length - decimalPosition - 1;
        long scale = (long)fractionalDigits - exponent;
        string digits = mantissa
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .TrimStart('-', '+')
            .TrimStart('0');
        if (digits.Length == 0)
        {
            return true;
        }

        long trailingZeros = digits.Length - digits.TrimEnd('0').Length;
        scale -= trailingZeros;
        decimal multipleOf = schema.GetProperty("multipleOf").GetDecimal();
        int allowedScale = (decimal.GetBits(multipleOf)[3] >> 16) & 0x7F;

        return scale <= allowedScale;
    }

    private static MetricResultDocument CreateDocument(MetricRecord[] metrics)
    {
        return new MetricResultDocument(
            MetricResultJson.SchemaVersion,
            new MetricTool("SourceGauge", "1.0.0"),
            new MetricRun(new MetricInput("project", "src/project.csproj")),
            metrics);
    }

    private static MetricRecord CreateMetric(string id, string projectId, string entityId, decimal value)
    {
        return new MetricRecord(
            id,
            "project",
            new MetricEntity("method", projectId, entityId, null),
            new MetricSource("src/Example.cs"),
            value,
            "count");
    }
}
