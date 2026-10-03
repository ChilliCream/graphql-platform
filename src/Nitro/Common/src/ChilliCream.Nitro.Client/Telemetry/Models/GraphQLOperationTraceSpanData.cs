namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record GraphQLOperationTraceSpanData(TraceDocument? Document, TraceOperation? Operation) : TraceSpanData;
