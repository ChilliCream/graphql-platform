namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;

internal sealed record TelemetryListColumn<TItem>(string Header, Func<TItem, string?> Value);
