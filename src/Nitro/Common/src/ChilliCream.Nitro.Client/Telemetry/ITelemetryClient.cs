using ChilliCream.Nitro.Client.Telemetry.Models;

namespace ChilliCream.Nitro.Client.Telemetry;

public interface ITelemetryClient
{
    Task<ConnectionPage<TraceRow>> ListTracesAsync(
        string workspaceId,
        OpenTelemetryFilterInput? filter,
        IReadOnlyList<string>? environments,
        IReadOnlyList<OpenTelemetrySpanKind>? spanKinds,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? first,
        string? after,
        CancellationToken cancellationToken);

    Task<Trace?> GetTraceAsync(
        string workspaceId,
        string traceId,
        string? spanId,
        string? seeker,
        CancellationToken cancellationToken);

    Task<ConnectionPage<LogRow>> ListLogsAsync(
        string workspaceId,
        OpenTelemetryFilterInput? filter,
        IReadOnlyList<string>? environments,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? first,
        string? after,
        CancellationToken cancellationToken);

    Task<Log?> GetLogAsync(string workspaceId, string id, CancellationToken cancellationToken);

    Task<ConnectionPage<ServiceRow>> ListServicesAsync(
        string workspaceId,
        string? search,
        OpenTelemetryFilterInput? filter,
        IReadOnlyList<string>? environments,
        DateTimeOffset from,
        DateTimeOffset to,
        int? first,
        string? after,
        CancellationToken cancellationToken);

    Task<ServiceRow?> GetServiceAsync(
        string workspaceId,
        string name,
        IReadOnlyList<string>? environments,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    Task<ConnectionPage<AttributeKeyRow>> ListAttributeKeysAsync(
        string workspaceId,
        OpenTelemetrySignalKind signal,
        IReadOnlyList<OpenTelemetryAttributeKind>? kinds,
        string? search,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? first,
        string? after,
        CancellationToken cancellationToken);

    Task<ConnectionPage<AttributeValue>> ListAttributeValuesAsync(
        string workspaceId,
        OpenTelemetrySignalKind signal,
        string key,
        OpenTelemetryAttributeKind? kind,
        string? search,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? first,
        string? after,
        CancellationToken cancellationToken);
}
