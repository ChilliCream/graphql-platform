using System.Diagnostics;
using OpenTelemetry.Trace;

namespace HotChocolate.Diagnostics;

internal sealed class FormatHttpResponseSpan(Activity activity) : SpanBase(activity)
{
    public static FormatHttpResponseSpan? Start(ActivitySource source)
    {
        var activity = source.StartActivity("Format HTTP Response");

        if (activity is null)
        {
            return null;
        }

        activity.MarkAsSuccess();

        return new FormatHttpResponseSpan(activity);
    }
}
