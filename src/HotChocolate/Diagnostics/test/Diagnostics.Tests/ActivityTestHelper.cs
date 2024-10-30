using System.Diagnostics;
#if NET8_0
using HotChocolate.Execution;
#endif
using HotChocolate.Utilities;

namespace HotChocolate.Diagnostics;

public static class ActivityTestHelper
{
    public static IDisposable CaptureActivities(out object activities)
    {
        var sync = new object();
        var listener = new ActivityListener();
        var root = new OrderedDictionary<string, object?>();
        var lookup = new Dictionary<Activity, OrderedDictionary<string, object?>>();
        Activity rootActivity = default!;

        listener.ShouldListenTo = source => source.Name.EqualsOrdinal("HotChocolate.Diagnostics");
        listener.ActivityStarted = a =>
        {
            lock (sync)
            {
                if (a.Parent is null &&
                    a.OperationName.EqualsOrdinal("ExecuteHttpRequest") &&
                    lookup.TryGetValue(rootActivity, out var parentData))
                {
                    RegisterActivity(a, parentData);
                    lookup[a] = (OrderedDictionary<string, object?>)a.GetCustomProperty("test.data")!;
                }

                if (a.Parent is not null &&
                    lookup.TryGetValue(a.Parent, out parentData))
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
        var data = (OrderedDictionary<string, object?>)activity.GetCustomProperty("test.data")!;

        if (data is null)
        {
            return;
        }

        data["OperationName"] = activity.OperationName;
        data["DisplayName"] = activity.DisplayName;
        data["Status"] = activity.Status;
        data["tags"] = activity.Tags;
        data["event"] = activity.Events.Select(t => new { t.Name, t.Tags, });
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
