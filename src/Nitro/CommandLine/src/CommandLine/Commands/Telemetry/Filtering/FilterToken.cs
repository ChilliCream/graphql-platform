namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal readonly record struct FilterToken(
    FilterTokenKind Kind,
    string Text,
    string Value,
    int Start,
    int End,
    bool HasWildcard = false);
