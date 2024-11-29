namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// Scalar <code>Scope</code> representation.
/// </summary>
public readonly record struct Scope
{
    /// <summary>
    /// Initializes a new instance of <see cref="Scope"/>.
    /// </summary>
    /// <param name="value">
    /// Scope value
    /// </param>
    public Scope(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        Value = value;
    }

    /// <summary>
    /// Retrieve scope value
    /// </summary>
    public string Value { get; }
}
