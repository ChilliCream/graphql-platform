using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace HotChocolate.Fusion.Diagnostics;

public static partial class ActivityTestHelper
{
    [GeneratedRegex(@" in (?<path>.+?):line (?<line>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex StackTracePathRegex();
    [GeneratedRegex(@"lambda_method\d+", RegexOptions.CultureInvariant)]
    private static partial Regex LambdaMethodRegex();

    public static IDisposable CaptureActivities(out object activities)
    {
        var exported = new List<Activity>();

        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddHotChocolateFusionInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Experimental.ModelContextProtocol")
            .SetSampler(new AlwaysOnSampler())
            .AddInMemoryExporter(exported)
            .Build()!;

        var capture = new Capture(tracerProvider, exported);
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

    private sealed class Capture : IDisposable
    {
        private readonly TracerProvider _tracerProvider;
        private readonly List<Activity> _exported;

        public Capture(TracerProvider tracerProvider, List<Activity> exported)
        {
            _tracerProvider = tracerProvider;
            _exported = exported;
        }

        [JsonProperty("source", Order = 0)]
        public OrderedDictionary<string, object?> Source
            => new()
            {
                // The first source name in stable (ordinal) order. Using the first exported
                // activity is not deterministic once nested transport spans are involved,
                // because the order in which sibling spans complete can vary between runs.
                ["name"] = _exported
                    .Select(a => a.Source.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .FirstOrDefault()
            };

        [JsonProperty("activities", Order = 1)]
        public IReadOnlyList<OrderedDictionary<string, object?>> Activities
        {
            get
            {
                var spanIds = new HashSet<ActivitySpanId>(_exported.Select(a => a.SpanId));
                var byParent = _exported
                    .GroupBy(a => a.ParentSpanId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(a => a.StartTimeUtc).ToList());

                return _exported
                    .Where(a => a.ParentSpanId == default || !spanIds.Contains(a.ParentSpanId))
                    .OrderBy(a => a.StartTimeUtc)
                    .Select(root => SerializeActivity(root, byParent))
                    .ToList();
            }
        }

        public void Dispose() => _tracerProvider.Dispose();
    }
}
