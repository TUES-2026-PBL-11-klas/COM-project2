using System.Diagnostics.Metrics;

namespace PM.Data.Observability
{
    public static class DataMetrics
    {
        public const string MeterName = "PM.Data";

        private static readonly Meter _meter = new(MeterName, "1.0.0");

        public static readonly Counter<long> UserAdded =
            _meter.CreateCounter<long>(
                "pm_data.user.add",
                description: "Number of users inserted into the database");

        public static readonly Counter<long> UserQueried =
            _meter.CreateCounter<long>(
                "pm_data.user.query",
                description: "Number of user lookup operations");

        public static readonly Counter<long> ConversationAdded =
            _meter.CreateCounter<long>(
                "pm_data.conversation.add",
                description: "Number of conversations inserted into the database");

        public static readonly Counter<long> ConversationQueried =
            _meter.CreateCounter<long>(
                "pm_data.conversation.query",
                description: "Number of conversation query operations");

        public static readonly Counter<long> MessageAdded =
            _meter.CreateCounter<long>(
                "pm_data.message.add",
                description: "Number of messages inserted into Cassandra");

        public static readonly Counter<long> MessageQueried =
            _meter.CreateCounter<long>(
                "pm_data.message.query",
                description: "Number of message query operations against Cassandra");

        public static readonly Histogram<double> OperationDuration =
            _meter.CreateHistogram<double>(
                "pm_data.operation.duration",
                unit: "ms",
                description: "Latency of PM.Data repository operations in milliseconds");
    }
}
