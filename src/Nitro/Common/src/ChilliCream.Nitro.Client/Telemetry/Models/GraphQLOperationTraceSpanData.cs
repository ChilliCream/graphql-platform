namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record GraphQLOperationTraceSpanData(GraphQLTraceDocument? Document, GraphQLTraceOperation? Operation)
    : TraceSpanData;
