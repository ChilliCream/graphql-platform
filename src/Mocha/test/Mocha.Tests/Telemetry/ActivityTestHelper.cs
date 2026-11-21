using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Mocha.Tests;

public static partial class ActivityTestHelper
{
    [GeneratedRegex("[0-9a-f]{32}", RegexOptions.CultureInvariant)]
    private static partial Regex GeneratedNameRegex();

    private static readonly HashSet<string> s_generatedTags =
    [
        "messaging.batch.id",
        "messaging.instance.id",
        "messaging.message.conversation_id",
        "messaging.message.id"
    ];

    public static IDisposable CaptureActivities(out Capture activities)
    {
        var exported = new List<Activity>();
        var quiescence = new QuiescenceProcessor();

        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddMochaInstrumentation()
            .SetSampler(new AlwaysOnSampler())
            .AddInMemoryExporter(exported)
            // Registered after the exporter so the exporter's OnEnd appends the span before
            // this processor signals idle.
            .AddProcessor(quiescence)
            .Build();

        var capture = new Capture(tracerProvider, exported, quiescence);
        activities = capture;
        return capture;
    }

    private static ActivityData SerializeActivity(
        Activity activity,
        IReadOnlyDictionary<ActivitySpanId, Activity> bySpanId,
        IReadOnlyDictionary<ActivitySpanId, List<Activity>> byParent)
        => new()
        {
            OperationName = ScrubName(activity.OperationName),
            DisplayName = ScrubName(activity.DisplayName),
            Kind = activity.Kind,
            Status = activity.Status,
            Tags = ScrubTags(activity.TagObjects),
            Events = activity.Events.Select(e => new { e.Name, Tags = e.Tags.ToList() }),
            Links = activity.Links
                .Select(link => bySpanId.TryGetValue(link.Context.SpanId, out var linked)
                    ? ScrubName(linked.DisplayName)
                    : "<not captured>")
                .Order(StringComparer.Ordinal),
            Activities = byParent.TryGetValue(activity.SpanId, out var children)
                ? children.Select(c => SerializeActivity(c, bySpanId, byParent)).ToList()
                : null
        };

    // Keep snapshot order tied to span content instead of scheduler-dependent timestamps.
    private static List<Activity> OrderActivities(IEnumerable<Activity> activities)
        => activities
            .OrderBy(a => ScrubName(a.OperationName), StringComparer.Ordinal)
            .ThenBy(a => ScrubName(a.DisplayName), StringComparer.Ordinal)
            .ThenBy(a => a.Kind)
            .ThenBy(a => a.Status)
            .ThenBy(GetContentKey, StringComparer.Ordinal)
            .ToList();

    private static string GetContentKey(Activity activity)
        => string.Concat(
            ScrubTags(activity.TagObjects)
                .Select(t => $"{t.Key}:{FormatValue(t.Value)};")
                .Concat(activity.Events.Select(e => $"{e.Name};")));

    private static string FormatValue(object? value)
        => value switch
        {
            null => "",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };

    // Ids are generated per message, batch and host, so they are replaced with a placeholder.
    private static IEnumerable<KeyValuePair<string, object?>> ScrubTags(
        IEnumerable<KeyValuePair<string, object?>> tags)
    {
        foreach (var tag in tags)
        {
            if (s_generatedTags.Contains(tag.Key) && tag.Value is not (null or ""))
            {
                yield return new KeyValuePair<string, object?>(tag.Key, "<id>");
            }
            else if (tag.Value is string or Uri)
            {
                yield return new KeyValuePair<string, object?>(tag.Key, ScrubName(tag.Value.ToString()!));
            }
            else
            {
                yield return tag;
            }
        }
    }

    // Reply endpoints are named after a generated id.
    private static string ScrubName(string name) => GeneratedNameRegex().Replace(name, "<id>");

    public sealed class Capture : IDisposable
    {
        private readonly TracerProvider _tracerProvider;
        private readonly List<Activity> _exported;
        private readonly QuiescenceProcessor _quiescence;
        private IReadOnlyList<Activity>? _settled;

        public Capture(
            TracerProvider tracerProvider,
            List<Activity> exported,
            QuiescenceProcessor quiescence)
        {
            _tracerProvider = tracerProvider;
            _exported = exported;
            _quiescence = quiescence;
        }

        // Spans are exported as they stop, and receive and consume spans finish on transport
        // threads after the awaited call returns. Wait until no spans are in flight so those
        // late spans are included, then read the snapshot.
        [JsonIgnore]
        public IReadOnlyList<Activity> Settled => _settled ??= CollectSettledActivities();

        private IReadOnlyList<Activity> CollectSettledActivities()
        {
            _quiescence.WaitForIdle(TimeSpan.FromSeconds(5));
            return _exported.ToArray();
        }

        [JsonProperty("sources", Order = 0)]
        public IReadOnlyList<string> Sources
            => Settled.Select(a => a.Source.Name).Distinct().Order(StringComparer.Ordinal).ToList();

        [JsonProperty("activities", Order = 1)]
        public IReadOnlyList<ActivityData> Activities
        {
            get
            {
                var exported = Settled;
                var bySpanId = exported.ToDictionary(a => a.SpanId);
                var byParent = exported
                    .GroupBy(a => a.ParentSpanId)
                    .ToDictionary(g => g.Key, OrderActivities);

                return OrderActivities(
                    exported.Where(a => a.ParentSpanId == default || !bySpanId.ContainsKey(a.ParentSpanId)))
                    .Select(root => SerializeActivity(root, bySpanId, byParent))
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

        [JsonProperty("links", Order = 6)]
        public required IEnumerable<string> Links { get; init; }

        [JsonProperty("activities", Order = 7, NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<ActivityData>? Activities { get; init; }
    }

    /// <summary>
    /// Tracks how many activities are in flight and lets a caller block until tracing is idle.
    /// </summary>
    public sealed class QuiescenceProcessor : BaseProcessor<Activity>
    {
        private readonly object _gate = new();
        private readonly ManualResetEventSlim _idle = new(initialState: true);
        private int _inFlight;

        public override void OnStart(Activity data)
        {
            lock (_gate)
            {
                _inFlight++;
                _idle.Reset();
            }
        }

        public override void OnEnd(Activity data)
        {
            lock (_gate)
            {
                if (--_inFlight == 0)
                {
                    _idle.Set();
                }
            }
        }

        // Bounded by the timeout so a stuck span surfaces as a snapshot mismatch rather than
        // hanging the test.
        public bool WaitForIdle(TimeSpan timeout) => _idle.Wait(timeout);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _idle.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
