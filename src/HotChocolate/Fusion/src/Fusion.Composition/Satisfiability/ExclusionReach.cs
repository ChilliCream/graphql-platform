namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// The reach of a visited object when the data of <paramref name="Unavailable"/> is unavailable.
/// </summary>
/// <param name="Unavailable">The schemas whose data is unavailable.</param>
/// <param name="Reach">
/// The schemas, among those a requirement check without that data may ask for, that can hold the
/// object at its position without that data.
/// </param>
/// <param name="ReachProvides">
/// The selection sets that the schemas of <paramref name="Reach"/> provide for the object, or
/// <c>null</c> if none provides one.
/// </param>
/// <param name="HolderAvailable">
/// <c>true</c> if a requirement check without that data may ask for the holding schema and that schema holds
/// the object without that data, through the call that holds it or as a schema of <paramref name="Reach"/>;
/// <c>false</c> otherwise, also when the holding call was made without that data but no such check asks for it.
/// </param>
/// <param name="ViewerOnly">
/// The schemas, among the holding schema and those of <paramref name="Reach"/>, that hold the object
/// without that data only through calls that depend on a call re-rooted at <c>Query.viewer</c>.
/// </param>
/// <param name="Arrival">
/// The calls of the access path that hold the object or its parent, by whether a requirement read at the
/// object without that data may take data from them.
/// </param>
internal readonly record struct ExclusionReach(
    SchemaSetRef Unavailable,
    SchemaSetRef Reach,
    SchemaSelectionSets? ReachProvides,
    bool HolderAvailable,
    SchemaSetRef ViewerOnly,
    ArrivalCalls Arrival);
