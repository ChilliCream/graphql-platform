namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record Trace(int? SpanCount, bool SpansTruncated, double TotalDuration, IReadOnlyList<TraceSpan> Spans);
