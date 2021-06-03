using System;

namespace HotChocolate.Execution.Instrumentation
{
    internal static class DateTimeOffsetExtensions
    {
        private const string _rfc3339DateTimeFormat =
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffZ";

        public static string ToRfc3339DateTimeString(this DateTimeOffset dateTimeOffset) =>
            dateTimeOffset.ToString(_rfc3339DateTimeFormat);
    }
}
