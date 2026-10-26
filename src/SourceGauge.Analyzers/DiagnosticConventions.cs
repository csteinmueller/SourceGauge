namespace SourceGauge.Analyzers
{
    /// <summary>
    /// Shared constants for all SourceGauge diagnostics.
    /// </summary>
    public static class DiagnosticConventions
    {
        /// <summary>Prefix for every SourceGauge diagnostic ID, e.g. <c>SG0001</c>.</summary>
        public const string IdPrefix = "SG";

        /// <summary>Diagnostic category used by all metric analyzers.</summary>
        public const string MetricsCategory = "Metrics";
    }
}
