namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// The call of the access path that a requirement read at an object without the unavailable data takes the
/// data of a schema from.
/// </summary>
internal enum ArrivalRead
{
    /// <summary>
    /// No call of the access path is read.
    /// </summary>
    None,

    /// <summary>
    /// The call that holds the object.
    /// </summary>
    Holder,

    /// <summary>
    /// The call that holds the parent and serves the field returning the object without requirements.
    /// </summary>
    Parent,

    /// <summary>
    /// A call of another schema that resolves the field returning the object from the parent, as one of the
    /// schemas that can resolve it when the schema holding the parent does not serve it without requirements.
    /// </summary>
    Route
}
