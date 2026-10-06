namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record GraphQLResolverTraceSpanData(GraphQLTraceSelection? Selection) : TraceSpanData;
