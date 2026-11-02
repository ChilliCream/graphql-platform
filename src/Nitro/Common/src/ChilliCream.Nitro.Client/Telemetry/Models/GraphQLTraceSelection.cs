namespace ChilliCream.Nitro.Client.Telemetry.Models;

public sealed record GraphQLTraceSelection(GraphQLTraceField Field, string? Name, string? Path, string? Type);
