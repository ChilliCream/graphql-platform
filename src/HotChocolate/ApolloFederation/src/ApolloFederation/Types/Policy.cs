namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// Scalar <code>Policy</code> representation.
/// </summary>
public readonly record struct Policy
{
    /// <summary>
    /// Initializes a new instance of <see cref="Policy"/>.
    /// </summary>
    /// <param name="value">
    /// Policy value
    /// </param>
    public Policy(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        Value = value;
    }

    /// <summary>
    /// Retrieve policy value
    /// </summary>
    public string Value { get; }
}
