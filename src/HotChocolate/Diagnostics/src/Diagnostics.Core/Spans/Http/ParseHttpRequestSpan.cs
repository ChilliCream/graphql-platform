using System.Diagnostics;
using OpenTelemetry.Trace;

namespace HotChocolate.Diagnostics;

internal sealed class ParseHttpRequestSpan(Activity activity) : SpanBase(activity)
{
    public static ParseHttpRequestSpan? Start(ActivitySource source)
    {
        var activity = source.StartActivity("Parse HTTP Request");

        if (activity is null)
        {
            return null;
        }

        activity.MarkAsSuccess();

        return new ParseHttpRequestSpan(activity);
    }

    public void RecordErrors(IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
        {
            Activity.RecordError(error);
        }

        Activity.MarkAsError();
    }
}
