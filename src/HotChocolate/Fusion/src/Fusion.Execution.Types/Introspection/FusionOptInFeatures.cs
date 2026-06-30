namespace HotChocolate.Fusion.Types.Introspection;

/// <summary>
/// Holds the set of opt-in feature names declared across the composed schema.
/// Stored as a schema feature when <c>EnableOptInFeatures</c> is <c>true</c>.
/// </summary>
internal sealed class FusionOptInFeatures : SortedSet<string>;
