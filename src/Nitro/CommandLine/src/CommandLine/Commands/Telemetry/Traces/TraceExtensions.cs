using System.Globalization;
using ChilliCream.Nitro.Client.Telemetry.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces;

internal static class TraceExtensions
{
    extension(TraceSpan span)
    {
        /// <summary>
        /// Gets a value indicating whether the span has an error status or records an exception event.
        /// </summary>
        public bool IsError
            => span.StatusCode.Contains("ERROR", StringComparison.OrdinalIgnoreCase)
            || span.Events.Any(static traceEvent =>
                traceEvent.Name.Equals("exception", StringComparison.OrdinalIgnoreCase)
            );
    }

    extension(double durationMs)
    {
        /// <summary>
        /// Formats a duration in milliseconds with up to three decimal places and the invariant culture.
        /// </summary>
        public string FormatDuration() => durationMs.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
