namespace HotChocolate;

/// <summary>
/// This enum declares the various state stores of Hot Chocolate.
/// </summary>
public enum StateKind
{
    /// <summary>
    /// The global state of a request.
    /// </summary>
    Global,

    /// <summary>
    /// The scoped state of a resolver.
    /// </summary>
    Scoped,

    /// <summary>
    /// The local state of a resolver pipeline.
    /// </summary>
    Local,

    /// <summary>
    /// The response state.
    /// </summary>
    Response,
}
