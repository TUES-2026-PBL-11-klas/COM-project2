using System.Diagnostics;

namespace PM.Data.Observability
{
    public static class DataActivitySource
    {
        public const string SourceName = "PM.Data";
        public const string Version    = "1.0.0";

        public static readonly ActivitySource Source = new(SourceName, Version);
    }
}
