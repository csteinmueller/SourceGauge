using Xunit;

namespace SourceGauge.Analyzers.Tests;

public class DiagnosticConventionsTests
{
    [Fact]
    public void IdPrefix_IsSG()
    {
        Assert.Equal("SG", DiagnosticConventions.IdPrefix);
    }
}
