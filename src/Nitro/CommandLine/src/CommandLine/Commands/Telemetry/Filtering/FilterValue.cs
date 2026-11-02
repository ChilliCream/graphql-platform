namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal readonly record struct FilterValue(
    FilterValueKind Kind,
    string Text,
    bool IsQuoted,
    bool HasWildcard,
    int Start,
    int End);
