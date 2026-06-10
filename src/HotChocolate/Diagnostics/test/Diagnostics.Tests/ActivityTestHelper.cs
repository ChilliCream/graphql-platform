using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace HotChocolate.Diagnostics;

public static partial class ActivityTestHelper
{
    [GeneratedRegex(@" in (?<path>.+?):line (?<line>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex StackTracePathRegex();
    [GeneratedRegex(@"lambda_method\d+", RegexOptions.CultureInvariant)]
    private static partial Regex LambdaMethodRegex();

    public static IDisposable CaptureActivities(out Capture activities)
    {
        var exported = new List<Activity>();
        var quiescence = new QuiescenceProcessor();

        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddHotChocolateInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Experimental.ModelContextProtocol")
            .SetSampler(new AlwaysOnSampler())
            .AddInMemoryExporter(exported)
            // Registered after the exporter so the exporter's OnEnd appends the span before
            // this processor signals idle.
            .AddProcessor(quiescence)
            .Build()!;

        var capture = new Capture(tracerProvider, exported, quiescence);
        activities = capture;
        return capture;
    }

    private static OrderedDictionary<string, object?> SerializeActivity(
        Activity activity,
        IReadOnlyDictionary<ActivitySpanId, List<Activity>> byParent)
    {
        var data = new OrderedDictionary<string, object?>
        {
            ["OperationName"] = activity.OperationName,
            ["DisplayName"] = activity.DisplayName,
            ["Kind"] = activity.Kind,
            ["Status"] = activity.Status,
            ["tags"] = activity.TagObjects,
            ["event"] = activity.Events.Select(e => new
            {
                e.Name,
                Tags = ScrubEventTags(e.Tags)
            })
        };

        if (byParent.TryGetValue(activity.SpanId, out var children) && children.Count > 0)
        {
            data["activities"] = children
                .Select(c => SerializeActivity(c, byParent))
                .ToList();
        }

        return data;
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

        // Spans are exported as they stop, and server-side spans can finish on background
        // continuations after the awaited call returns. Wait until no spans are in flight so
        // those late spans are included, then read the snapshot.
        private IReadOnlyList<Activity> Settled => _settled ??= CollectSettledActivities();

        private IReadOnlyList<Activity> CollectSettledActivities()
        {
            _quiescence.WaitForIdle(TimeSpan.FromSeconds(5));
            return _exported.ToArray();
        }

        [JsonIgnore]
        public IReadOnlyList<Activity> Exported => _exported;

        [JsonProperty("source", Order = 0)]
        public OrderedDictionary<string, object?> Source
            => new()
            {
                // The first source name in stable (ordinal) order. Using the first exported
                // activity is not deterministic once nested transport spans are involved,
                // because the order in which sibling spans complete can vary between runs.
                ["name"] = Settled
                    .Select(a => a.Source.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .FirstOrDefault()
            };

        [JsonProperty("activities", Order = 1)]
        public IReadOnlyList<OrderedDictionary<string, object?>> Activities
        {
            get
            {
                var exported = Settled;
                var spanIds = new HashSet<ActivitySpanId>(exported.Select(a => a.SpanId));
                var byParent = exported
                    .GroupBy(a => a.ParentSpanId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(a => a.StartTimeUtc).ToList());

                return exported
                    .Where(a => a.ParentSpanId == default || !spanIds.Contains(a.ParentSpanId))
                    .OrderBy(a => a.StartTimeUtc)
                    .Select(root => SerializeActivity(root, byParent))
                    .ToList();
            }
        }

        public void Dispose() => _tracerProvider.Dispose();
    }

    /// <summary>
    /// Tracks how many activities are currently in flight and lets a caller block until
    /// tracing has gone idle, so a captured trace is read only after every span (including
    /// server-side spans that complete after the awaited call returns) has finished.
    /// </summary>
    private sealed class QuiescenceProcessor : BaseProcessor<Activity>
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

        // Block until no spans are in flight, so late-completing server-side spans are
        // captured. Bounded by the timeout so a stuck span surfaces as a snapshot mismatch
        // rather than hanging the test.
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
