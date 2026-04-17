using System.Diagnostics;

namespace PM.Data.Observability
{
    /// <summary>
    /// Shared ActivitySource for all PM.Data repository operations.
    /// Registered in PM.API's OpenTelemetry WithTracing() call so spans
    /// flow automatically to Grafana Alloy → Grafana Tempo.
    /// </summary>
    public static class DataActivitySource
    {
        public const string SourceName = "PM.Data";
        public const string Version    = "1.0.0";

        public static readonly ActivitySource Source = new(SourceName, Version);
    }
}
