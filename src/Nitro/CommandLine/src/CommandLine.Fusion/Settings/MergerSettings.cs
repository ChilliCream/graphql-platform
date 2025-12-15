using HotChocolate.Fusion.Options;

namespace ChilliCream.Nitro.CommandLine.Fusion.Settings;

internal sealed record MergerSettings
{
    public bool? AddFusionDefinitions { get; init; }

    public DirectiveMergeBehavior? CacheControlMergeBehavior { get; init; }

    public bool? EnableGlobalObjectIdentification { get; init; }

    public bool? RemoveUnreferencedDefinitions { get; init; }

    public DirectiveMergeBehavior? TagMergeBehavior { get; init; }
}
