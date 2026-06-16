namespace ChilliCream.Nitro.Client.FusionConfiguration;

/// <summary>
/// Represents the composition settings stored on a stage.
/// </summary>
public sealed record StageCompositionSettings
{
    public DirectiveMergeBehavior? CacheControlMergeBehavior { get; init; }

    public bool? EnableGlobalObjectIdentification { get; init; }

    public IReadOnlyList<string>? ExcludeByTag { get; init; }

    public bool? RemoveUnreferencedDefinitions { get; init; }

    public DirectiveMergeBehavior? TagMergeBehavior { get; init; }
}
