using ChilliCream.Nitro.Client.Telemetry.Models;

namespace ChilliCream.Nitro.Client.Telemetry;

internal sealed class TelemetryClient(IApiClient apiClient) : ITelemetryClient
{
    private static readonly OpenTelemetrySpanKind[] s_defaultSpanKinds =
    [
        OpenTelemetrySpanKind.Server,
        OpenTelemetrySpanKind.Consumer
    ];

    public async Task<ConnectionPage<TraceRow>> ListTracesAsync(
        string workspaceId,
        OpenTelemetryFilterInput? filter,
        IReadOnlyList<string>? environments,
        IReadOnlyList<OpenTelemetrySpanKind>? spanKinds,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? first,
        string? after,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListTraceCommandQuery.ExecuteAsync(
            workspaceId,
            filter,
            environments,
            spanKinds ?? s_defaultSpanKinds,
            from,
            to,
            first,
            after,
            cancellationToken);

        var connection = OperationResultHelper.EnsureData(result).WorkspaceById?.Spans;
        var items = connection?.Edges?.Select(static edge => edge.Node.ToTraceRow()).ToArray() ?? [];

        return new ConnectionPage<TraceRow>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }

    public async Task<Trace?> GetTraceAsync(
        string workspaceId,
        string traceId,
        string? spanId,
        string? seeker,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ShowTraceCommandQuery.ExecuteAsync(
            workspaceId,
            traceId,
            spanId,
            seeker,
            cancellationToken);

        var trace = OperationResultHelper.EnsureData(result).WorkspaceById?.TraceById;

        return trace is null
            ? null
            : new Trace(
                trace.SpanCount,
                trace.SpansTruncated,
                trace.TotalDuration,
                trace.Spans.Select(static span => span.ToTraceSpan()).ToArray());
    }

    public async Task<ConnectionPage<LogRow>> ListLogsAsync(
        string workspaceId,
        OpenTelemetryFilterInput? filter,
        IReadOnlyList<string>? environments,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? first,
        string? after,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListLogCommandQuery.ExecuteAsync(
            workspaceId,
            filter,
            environments,
            from,
            to,
            first,
            after,
            cancellationToken);

        var connection = OperationResultHelper.EnsureData(result).WorkspaceById?.Logs;
        var items = connection?.Edges?.Select(static edge => edge.Node.ToLogRow()).ToArray() ?? [];

        return new ConnectionPage<LogRow>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }

    public async Task<Log?> GetLogAsync(string workspaceId, string id, CancellationToken cancellationToken)
    {
        var result = await apiClient.ShowLogCommandQuery.ExecuteAsync(workspaceId, id, cancellationToken);
        var log = OperationResultHelper.EnsureData(result).WorkspaceById?.LogById;

        return log?.ToLog();
    }

    public async Task<ConnectionPage<ServiceRow>> ListServicesAsync(
        string workspaceId,
        string? search,
        OpenTelemetryFilterInput? filter,
        IReadOnlyList<string>? environments,
        DateTimeOffset from,
        DateTimeOffset to,
        int? first,
        string? after,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListServiceCommandQuery.ExecuteAsync(
            workspaceId,
            search,
            filter,
            environments,
            from,
            to,
            first,
            after,
            cancellationToken);

        var connection = OperationResultHelper.EnsureData(result).WorkspaceById?.Services;
        var items = connection?.Edges?.Select(static edge => edge.Node.ToServiceRow()).ToArray() ?? [];

        return new ConnectionPage<ServiceRow>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }

    public async Task<ServiceRow?> GetServiceAsync(
        string workspaceId,
        string name,
        IReadOnlyList<string>? environments,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ShowServiceCommandQuery.ExecuteAsync(
            workspaceId,
            name,
            environments,
            from,
            to,
            cancellationToken);

        var service = OperationResultHelper.EnsureData(result).WorkspaceById?.Service;

        return service?.ToServiceRow();
    }

    public async Task<ConnectionPage<AttributeKeyRow>> ListAttributeKeysAsync(
        string workspaceId,
        OpenTelemetrySignalKind signal,
        IReadOnlyList<OpenTelemetryAttributeKind>? kinds,
        string? search,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? first,
        string? after,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListAttributeKeyCommandQuery.ExecuteAsync(
            workspaceId,
            signal,
            kinds,
            search,
            from,
            to,
            first,
            after,
            cancellationToken);

        var connection = OperationResultHelper.EnsureData(result).WorkspaceById?.AttributeKeys;
        var items =
            connection
                ?.Edges?.Select(static edge => new AttributeKeyRow(edge.Node.Kind.ToString(), edge.Node.Path))
                .ToArray()
            ?? [];

        return new ConnectionPage<AttributeKeyRow>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }

    public async Task<ConnectionPage<AttributeValue>> ListAttributeValuesAsync(
        string workspaceId,
        OpenTelemetrySignalKind signal,
        string key,
        OpenTelemetryAttributeKind? kind,
        string? search,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? first,
        string? after,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListAttributeValueCommandQuery.ExecuteAsync(
            workspaceId,
            signal,
            key,
            kind,
            search,
            from,
            to,
            first,
            after,
            cancellationToken);

        var connection = OperationResultHelper.EnsureData(result).WorkspaceById?.AttributeValues;
        var items =
            connection
                ?.Edges?.Select(static edge => new AttributeValue(
                    edge.Node.Boolean,
                    edge.Node.Float,
                    edge.Node.Int,
                    edge.Node.String))
                .ToArray()
            ?? [];

        return new ConnectionPage<AttributeValue>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }
}

file static class Extensions
{
    extension(IListTraceCommandQuery_WorkspaceById_Spans_Edges_Node span)
    {
        public TraceRow ToTraceRow()
        {
            return new TraceRow(
                span.TraceId,
                span.SpanId,
                span.Seeker,
                span.SpanName,
                span.SpanKind,
                span.Duration,
                span.Epoch,
                span.StatusCode,
                span.ResourceAttributes.GetServiceName());
        }
    }

    extension(IShowTraceCommandQuery_WorkspaceById_TraceById_Spans span)
    {
        public TraceSpan ToTraceSpan()
        {
            return new TraceSpan(
                span.SpanId,
                span.ParentSpanId,
                span.SpanName,
                span.SpanKind,
                span.Duration,
                span.Epoch,
                span.StatusCode,
                span.StatusMessage,
                span.ResourceAttributes.Select(static attribute => new TelemetryAttribute(
                        attribute.Key,
                        attribute.Value))
                    .ToArray(),
                span.SpanAttributes.Select(static attribute => new TelemetryAttribute(attribute.Key, attribute.Value))
                    .ToArray(),
                span.Events.Select(static traceEvent => new TraceEvent(
                        traceEvent.Name,
                        traceEvent.Epoch,
                        traceEvent
                            .Attributes.Select(static attribute => new TelemetryAttribute(
                                attribute.Key,
                                attribute.Value))
                            .ToArray()))
                    .ToArray(),
                span.ToTraceSpanData());
        }

        private TraceSpanData? ToTraceSpanData()
        {
            return span switch
            {
                IShowTraceCommand_Span_OpenTelemetryHttpClientSpan clientSpan => new HttpTraceSpanData(
                    clientSpan.Http?.Flavor,
                    clientSpan.Http?.Method,
                    clientSpan.Http?.Scheme,
                    clientSpan.Http?.StatusCode,
                    clientSpan.Http?.Url,
                    clientSpan.Http?.UserAgent),
                IShowTraceCommand_Span_OpenTelemetryHttpServerSpan serverSpan => new HttpTraceSpanData(
                    serverSpan.Http?.Flavor,
                    serverSpan.Http?.Method,
                    serverSpan.Http?.Scheme,
                    serverSpan.Http?.StatusCode,
                    serverSpan.Http?.Url,
                    serverSpan.Http?.UserAgent),
                IShowTraceCommand_Span_OpenTelemetryDbSpan databaseSpan => new DatabaseTraceSpanData(
                    databaseSpan.Db?.ConnectionString,
                    databaseSpan.Db?.Instance,
                    databaseSpan.Db?.Name,
                    databaseSpan.Db?.Operation,
                    databaseSpan.Db?.Statement,
                    databaseSpan.Db?.System,
                    databaseSpan.Db?.Url,
                    databaseSpan.Db?.User),
                IShowTraceCommand_Span_OpenTelemetryGraphQLOperationSpan operationSpan =>
                    new GraphQLOperationTraceSpanData(
                        operationSpan.Document is { } document
                            ? new GraphQLTraceDocument(document.Body, document.Id)
                            : null,
                        operationSpan.Operation is { } operation
                            ? new GraphQLTraceOperation(operation.Hash, operation.Kind, operation.Name)
                            : null),
                IShowTraceCommand_Span_OpenTelemetryGraphQLResolverSpan resolverSpan =>
                    new GraphQLResolverTraceSpanData(
                        resolverSpan.Selection is { } selection
                            ? new GraphQLTraceSelection(
                                new GraphQLTraceField(
                                    selection.Field.Coordinate,
                                    selection.Field.DeclaringType,
                                    selection.Field.Name),
                                selection.Name,
                                selection.Path,
                                selection.Type)
                            : null),
                _ => null
            };
        }
    }

    extension(IListLogCommandQuery_WorkspaceById_Logs_Edges_Node log)
    {
        public LogRow ToLogRow()
        {
            return new LogRow(
                log.Id,
                log.Epoch,
                log.SeverityText,
                log.SeverityNumber,
                log.Body,
                log.TraceId,
                log.SpanId,
                log.ResourceAttributes.GetServiceName());
        }
    }

    extension(IShowLogCommandQuery_WorkspaceById_LogById log)
    {
        public Log ToLog()
        {
            return new Log(
                log.Id,
                log.Epoch,
                log.SeverityText,
                log.SeverityNumber,
                log.Body,
                log.TraceId,
                log.SpanId,
                new LogBodyDetail(log.BodyDetail.Json, log.BodyDetail.Kind.ToString(), log.BodyDetail.Message),
                log.LogAttributes.Select(static attribute => attribute.ToTypedTelemetryAttribute()).ToArray(),
                log.ResourceAttributes.Select(static attribute => new TelemetryAttribute(
                        attribute.Key,
                        attribute.Value))
                    .ToArray(),
                log.Scope is { } scope
                    ? new TelemetryScope(
                        scope.Name,
                        scope.SchemaUrl,
                        scope.Version,
                        scope.Attributes.Select(static attribute => attribute.ToTypedTelemetryAttribute()).ToArray())
                    : null);
        }
    }

    extension(IShowLogCommand_OpenTelemetryAttribute attribute)
    {
        public TypedTelemetryAttribute ToTypedTelemetryAttribute()
        {
            return attribute switch
            {
                IShowLogCommand_OpenTelemetryAttribute_OpenTelemetryBoolAttribute booleanAttribute =>
                    new TypedTelemetryAttribute(booleanAttribute.Key, booleanAttribute.Boolean, null, null, null),
                IShowLogCommand_OpenTelemetryAttribute_OpenTelemetryFloatAttribute floatAttribute =>
                    new TypedTelemetryAttribute(floatAttribute.Key, null, floatAttribute.Float, null, null),
                IShowLogCommand_OpenTelemetryAttribute_OpenTelemetryLongAttribute longAttribute =>
                    new TypedTelemetryAttribute(longAttribute.Key, null, null, longAttribute.Long, null),
                IShowLogCommand_OpenTelemetryAttribute_OpenTelemetryStringAttribute stringAttribute =>
                    new TypedTelemetryAttribute(stringAttribute.Key, null, null, null, stringAttribute.String),
                _ => new TypedTelemetryAttribute(attribute.Key, null, null, null, null)
            };
        }
    }

    extension(IListServiceCommandQuery_WorkspaceById_Services_Edges_Node service)
    {
        public ServiceRow ToServiceRow()
        {
            return new ServiceRow(
                service.Name,
                service.EnvironmentNames,
                service
                    .VersionMarkers?.Select(static marker => new ServiceVersionMarker(
                        marker.FirstSeenAt,
                        marker.Version))
                    .ToArray()
                    ?? []);
        }
    }

    extension(IShowServiceCommandQuery_WorkspaceById_Service service)
    {
        public ServiceRow ToServiceRow()
        {
            return new ServiceRow(
                service.Name,
                service.EnvironmentNames,
                service
                    .VersionMarkers?.Select(static marker => new ServiceVersionMarker(
                        marker.FirstSeenAt,
                        marker.Version))
                    .ToArray()
                    ?? []);
        }
    }

    extension(IReadOnlyList<IListTraceCommandQuery_WorkspaceById_Spans_Edges_Node_ResourceAttributes> attributes)
    {
        public string GetServiceName()
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Key == "service.name")
                {
                    return attribute.Value;
                }
            }

            return string.Empty;
        }
    }

    extension(IReadOnlyList<IListLogCommandQuery_WorkspaceById_Logs_Edges_Node_ResourceAttributes> attributes)
    {
        public string GetServiceName()
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Key == "service.name")
                {
                    return attribute.Value;
                }
            }

            return string.Empty;
        }
    }
}
