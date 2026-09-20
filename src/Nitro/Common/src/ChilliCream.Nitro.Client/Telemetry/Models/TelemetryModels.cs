namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record TraceRow(
    string TraceId,
    string SpanId,
    string Seeker,
    string SpanName,
    string SpanKind,
    double DurationMs,
    double Start,
    string StatusCode,
    string ServiceName);

public sealed record Trace(
    int? SpanCount,
    bool SpansTruncated,
    double TotalDuration,
    IReadOnlyList<TraceSpan> Spans);

public sealed record TraceSpan(
    string SpanId,
    string ParentSpanId,
    string SpanName,
    string SpanKind,
    double DurationMs,
    double Start,
    string StatusCode,
    string StatusMessage,
    IReadOnlyList<TelemetryAttribute> ResourceAttributes,
    IReadOnlyList<TelemetryAttribute> SpanAttributes,
    IReadOnlyList<TraceEvent> Events,
    TraceSpanData? Data);

public abstract record TraceSpanData;

public sealed record HttpTraceSpanData(
    string? Flavor,
    string? Method,
    string? Scheme,
    int? StatusCode,
    string? Url,
    string? UserAgent) : TraceSpanData;

public sealed record DatabaseTraceSpanData(
    string? ConnectionString,
    string? Instance,
    string? Name,
    string? Operation,
    string? Statement,
    string? System,
    string? Url,
    string? User) : TraceSpanData;

public sealed record GraphQLOperationTraceSpanData(
    TraceDocument? Document,
    TraceOperation? Operation) : TraceSpanData;

public sealed record GraphQLResolverTraceSpanData(TraceSelection? Selection) : TraceSpanData;

public sealed record TraceDocument(string? Body, string? Id);

public sealed record TraceOperation(string? Hash, string? Kind, string? Name);

public sealed record TraceSelection(
    TraceField Field,
    string? Name,
    string? Path,
    string? Type);

public sealed record TraceField(string? Coordinate, string? DeclaringType, string? Name);

public sealed record TraceEvent(
    string Name,
    double Start,
    IReadOnlyList<TelemetryAttribute> Attributes);

public sealed record TelemetryAttribute(string Key, string Value);

public sealed record LogRow(
    string Id,
    double Start,
    string SeverityText,
    int SeverityNumber,
    string Body,
    string TraceId,
    string SpanId,
    string ServiceName);

public sealed record Log(
    string Id,
    double Start,
    string SeverityText,
    int SeverityNumber,
    string Body,
    string TraceId,
    string SpanId,
    LogBodyDetail BodyDetail,
    IReadOnlyList<TypedTelemetryAttribute> LogAttributes,
    IReadOnlyList<TelemetryAttribute> ResourceAttributes,
    TelemetryScope? Scope);

public sealed record LogBodyDetail(string? Json, string Kind, string? Message);

public sealed record TypedTelemetryAttribute(
    string Key,
    bool? Boolean,
    double? Float,
    long? Long,
    string? String);

public sealed record TelemetryScope(
    string? Name,
    string? SchemaUrl,
    string? Version,
    IReadOnlyList<TypedTelemetryAttribute> Attributes);

public sealed record ServiceRow(
    string Name,
    IReadOnlyList<string> EnvironmentNames,
    IReadOnlyList<ServiceVersionMarker> VersionMarkers);

public sealed record ServiceVersionMarker(DateTimeOffset FirstSeenAt, string Version);

public sealed record AttributeKeyRow(string Kind, string Path);

public sealed record AttributeValue(bool? Boolean, double? Float, int? Int, string? String);
