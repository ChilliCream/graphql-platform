namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record GraphQLResolverTraceSpanData(TraceSelection? Selection) : TraceSpanData;
