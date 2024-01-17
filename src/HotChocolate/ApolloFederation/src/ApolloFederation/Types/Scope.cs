namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// Scalar <code>Scope</code> representation.
/// </summary>
public sealed class Scope
{
    /// <summary>
    /// Initializes a new instance of <see cref="Scope"/>.
    /// </summary>
    /// <param name="value">
    /// Scope value
    /// </param>
    public Scope(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Retrieve scope value
    /// </summary>
    public string Value { get; }
}