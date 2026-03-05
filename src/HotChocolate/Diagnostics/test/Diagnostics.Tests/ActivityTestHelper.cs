using System.Diagnostics;
using System.Text.RegularExpressions;
using HotChocolate.Utilities;

namespace HotChocolate.Diagnostics;

public static class ActivityTestHelper
{
    private static readonly Regex _stackTracePathRegex =
        new(@" in (?<path>.+?):line (?<line>\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IDisposable CaptureActivities(out object activities)
    {
        var sync = new object();
        var listener = new ActivityListener();
        var root = new OrderedDictionary<string, object?>();
        var lookup = new Dictionary<Activity, OrderedDictionary<string, object?>>();
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
                yield return new KeyValuePair<string, object?>(
                    tag.Key,
                    _stackTracePathRegex.Replace(stackTrace, match =>
                    {
                        var fileName = System.IO.Path.GetFileName(match.Groups["path"].Value);
                        var lineNumber = match.Groups["line"].Value;
                        return $" in {fileName}:line {lineNumber}";
                    }));
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
