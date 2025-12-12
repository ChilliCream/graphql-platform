namespace ChilliCream.Nitro.CommandLine.Fusion.Settings;

internal sealed record PreprocessorSettings
{
    public bool? InferKeysFromLookups { get; init; }

    public bool? InheritInterfaceKeys { get; init; }
}
