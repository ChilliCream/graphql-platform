namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// The calls of the access path that hold a visited object, or its parent, when the data of some schemas is
/// unavailable.
/// </summary>
/// <param name="HolderReadable">
/// <c>true</c> if a requirement check without that data may read requirement data from the holding schema and
/// the holding call is made with that data, but does not depend on the call that resolves a requiring field of
/// an unavailable schema at the object; otherwise, <c>false</c>.
/// </param>
/// <param name="HolderPending">
/// <c>true</c> if such a holding call depends on the last call of the only unavailable schema on the path, which
/// resolves its requiring field as long as that schema serves every field below it; otherwise, <c>false</c>.
/// </param>
/// <param name="ParentReadable">
/// The schema whose call holds the parent and serves the field returning the object without requirements, if a
/// requirement check without that data at the object reads requirement data from it, it is not in the reach, and
/// the call does not depend on the call that resolves the requiring field; otherwise, the empty set.
/// </param>
/// <param name="RouteReadable">
/// The schemas, other than the holding schema, that resolve the field returning the object from the parent when
/// the schema holding the parent does not serve that field without requirements, if a check without the data of a
/// single schema reads requirement data from them and they are not in the reach; otherwise, the empty set.
/// </param>
/// <param name="BelowReRootedViewer">
/// <c>true</c> if a viewer field on the path below the last node lookup is re-rooted at <c>Query.viewer</c>, in
/// which case no call is read; otherwise, <c>false</c>.
/// </param>
internal readonly record struct ArrivalCalls(
    bool HolderReadable,
    bool HolderPending,
    SchemaSetRef ParentReadable,
    SchemaSetRef RouteReadable,
    bool BelowReRootedViewer);
