using System.Diagnostics;
using System.Text.RegularExpressions;
using HotChocolate.Utilities;

namespace HotChocolate.Diagnostics;

public static partial class ActivityTestHelper
{
    [GeneratedRegex(@" in (?<path>.+?):line (?<line>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex StackTracePathRegex();
    [GeneratedRegex(@"lambda_method\d+", RegexOptions.CultureInvariant)]
    private static partial Regex LambdaMethodRegex();

    public static IDisposable CaptureActivities(out object activities)
    {
        var sync = new object();
        var listener = new ActivityListener();
        var root = new OrderedDictionary<string, object?>();
        var lookup = new Dictionary<Activity, OrderedDictionary<string, object?>>();
        var spanLookup = new Dictionary<ActivitySpanId, OrderedDictionary<string, object?>>();
        Activity rootActivity = null!;

        listener.ShouldListenTo = source => source.Name.EqualsOrdinal("HotChocolate.Diagnostics");
        listener.ActivityStarted = a =>
        {
            lock (sync)
            {
                if (a.Parent is null
                    && a.OperationName.EqualsOrdinal("ExecuteHttpRequest")
                    && lookup.TryGetValue(rootActivity, out var parentData))
                {
                    RegisterActivity(a, parentData);
                    lookup[a] = (OrderedDictionary<string, object?>)a.GetCustomProperty("test.data")!;
                }

                if (a.Parent is not null
                    && lookup.TryGetValue(a.Parent, out parentData))
                {
                    RegisterActivity(a, parentData);
                    lookup[a] = (OrderedDictionary<string, object?>)a.GetCustomProperty("test.data")!;
                    spanLookup[a.SpanId] = (OrderedDictionary<string, object?>)a.GetCustomProperty("test.data")!;
                    return;
                }

                if (a.Parent is null
                    && a.ParentSpanId != default
                    && spanLookup.TryGetValue(a.ParentSpanId, out parentData))
                {
                    RegisterActivity(a, parentData);
                    lookup[a] = (OrderedDictionary<string, object?>)a.GetCustomProperty("test.data")!;
                    spanLookup[a.SpanId] = (OrderedDictionary<string, object?>)a.GetCustomProperty("test.data")!;
                }
            }
        };
        listener.ActivityStopped = SerializeActivity;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
            ActivitySamplingResult.AllData;
        ActivitySource.AddActivityListener(listener);

        rootActivity = HotChocolateActivitySource.Source.StartActivity()!;
        rootActivity.SetCustomProperty("test.data", root);
        lookup[rootActivity] = root;
        spanLookup[rootActivity.SpanId] = root;

        activities = root;
        return new Session(rootActivity, listener);
    }

    private static void RegisterActivity(
        Activity activity,
        OrderedDictionary<string, object?> parent)
    {
        if (!(parent.TryGetValue("activities", out var value) && value is List<object> children))
        {
            children = [];
            parent["activities"] = children;
        }

        var data = new OrderedDictionary<string, object?>();
        activity.SetCustomProperty("test.data", data);
        SerializeActivity(activity);
        children.Add(data);
    }

    private static void SerializeActivity(Activity activity)
    {
        var data = (OrderedDictionary<string, object?>?)activity.GetCustomProperty("test.data");

        if (data is null)
        {
            return;
        }

        data["OperationName"] = activity.OperationName;
        data["DisplayName"] = activity.DisplayName;
        data["Status"] = activity.Status;
        data["tags"] = activity.TagObjects;
        data["event"] = activity.Events.Select(t => new
        {
            t.Name,
            Tags = ScrubEventTags(t.Tags)
        });
    }

    private static IEnumerable<KeyValuePair<string, object?>> ScrubEventTags(
        IEnumerable<KeyValuePair<string, object?>>? tags)
    {
        if (tags is null)
        {
            yield break;
        }

        foreach (var tag in tags)
        {
            if (tag.Value is string stackTrace
                && (tag.Key.Equals("exception.stacktrace", StringComparison.Ordinal)
                    || tag.Key.EndsWith(".stacktrace", StringComparison.Ordinal)))
            {
                var scrubbedStackTrace = StackTracePathRegex().Replace(stackTrace, match =>
                {
                    var fileName = System.IO.Path.GetFileName(match.Groups["path"].Value);
                    return $" in {fileName}";
                });

                yield return new KeyValuePair<string, object?>(
                    tag.Key,
                    LambdaMethodRegex().Replace(scrubbedStackTrace, "lambda_method"));
            }
            else
            {
                yield return tag;
            }
        }
    }

    private sealed class Session : IDisposable
    {
        private readonly Activity _activity;
        private readonly ActivityListener _listener;

        public Session(Activity activity, ActivityListener listener)
        {
            _activity = activity;
            _listener = listener;
        }

        public void Dispose()
        {
            _activity.Dispose();
            _listener.Dispose();
        }
    }
}
