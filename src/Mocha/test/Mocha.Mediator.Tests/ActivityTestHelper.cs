using System.Diagnostics;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Mocha.Mediator.Tests;

public static class ActivityTestHelper
{
    public static IDisposable CaptureActivities(out Capture activities)
    {
        var exported = new List<Activity>();

        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddMochaMediatorInstrumentation()
            .SetSampler(new AlwaysOnSampler())
            .AddInMemoryExporter(exported)
            .Build();

        var capture = new Capture(tracerProvider, exported);
        activities = capture;
        return capture;
    }

    private static ActivityData SerializeActivity(
        Activity activity,
        IReadOnlyDictionary<ActivitySpanId, List<Activity>> byParent)
        => new()
        {
            OperationName = activity.OperationName,
            DisplayName = activity.DisplayName,
            Kind = activity.Kind,
            Status = activity.Status,
            Tags = activity.TagObjects,
            Events = activity.Events.Select(e => new { e.Name, Tags = e.Tags.ToList() }),
            Activities = byParent.TryGetValue(activity.SpanId, out var children)
                ? children.Select(c => SerializeActivity(c, byParent)).ToList()
                : null
        };

    // Spans are exported when they stop, so a parent is exported after its children.
    private static List<Activity> OrderActivities(IEnumerable<Activity> activities)
        => activities.OrderBy(a => a.StartTimeUtc).ToList();

    public sealed class Capture : IDisposable
    {
        private readonly TracerProvider _tracerProvider;
        private readonly List<Activity> _exported;

        public Capture(TracerProvider tracerProvider, List<Activity> exported)
        {
            _tracerProvider = tracerProvider;
            _exported = exported;
        }

        [JsonProperty("sources", Order = 0)]
        public IReadOnlyList<string> Sources
            => _exported.Select(a => a.Source.Name).Distinct().Order(StringComparer.Ordinal).ToList();

        [JsonProperty("activities", Order = 1)]
        public IReadOnlyList<ActivityData> Activities
        {
            get
            {
                var exported = _exported.ToArray();
                var spanIds = exported.Select(a => a.SpanId).ToHashSet();
                var byParent = exported
                    .GroupBy(a => a.ParentSpanId)
                    .ToDictionary(g => g.Key, OrderActivities);

                return OrderActivities(
                    exported.Where(a => a.ParentSpanId == default || !spanIds.Contains(a.ParentSpanId)))
                    .Select(root => SerializeActivity(root, byParent))
                    .ToList();
            }
        }

        public void Dispose() => _tracerProvider.Dispose();
    }

    public sealed class ActivityData
    {
        [JsonProperty(Order = 0)]
        public required string OperationName { get; init; }

        [JsonProperty(Order = 1)]
        public required string DisplayName { get; init; }

        [JsonProperty(Order = 2)]
        public required ActivityKind Kind { get; init; }

        [JsonProperty(Order = 3)]
        public required ActivityStatusCode Status { get; init; }

        [JsonProperty("tags", Order = 4)]
        public required IEnumerable<KeyValuePair<string, object?>> Tags { get; init; }

        [JsonProperty("events", Order = 5)]
        public required IEnumerable<object> Events { get; init; }

        [JsonProperty("activities", Order = 6, NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<ActivityData>? Activities { get; init; }
    }
}
