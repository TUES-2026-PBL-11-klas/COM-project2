using System.Diagnostics;

namespace PM.Data.Observability
{
    public static class ActivityExtensions
    {
        public static void RecordException(this Activity? activity, Exception exception)
        {
            if (activity is null)
            {
                return;
            }

            activity.SetTag("exception.type", exception.GetType().FullName);
            activity.SetTag("exception.message", exception.Message);
            activity.SetTag("exception.stacktrace", exception.StackTrace);
        }
    }
}
