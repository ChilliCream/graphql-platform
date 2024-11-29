namespace HotChocolate.Types.Descriptors;

/// <summary>
/// Query Documentation.
/// </summary>
public class QueryWithDocumentation
{
    /// <summary>
    /// Foo Documentation.
    /// </summary>
    /// <param name="bar">
    /// Bar Documentation.
    /// </param>
    /// <returns>
    /// Bar Returns Documentation.
    /// </returns>
    public string? Foo(string? bar) => bar;

    /// <summary>
    /// This is a
    /// multiline summary
    ///
    /// with a newline in between.
    /// </summary>
    /// Note: The returns is left intentionally empty
    /// <returns></returns>
    public string? Baz() => string.Empty;

    /// <summary>
    /// This is a single line summary.
    /// </summary>
    public string? Qux() => string.Empty;
}
