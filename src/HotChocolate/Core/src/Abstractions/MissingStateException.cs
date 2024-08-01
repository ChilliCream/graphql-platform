using HotChocolate.Properties;

namespace HotChocolate;

/// <summary>
/// This exception can be thrown if a feature that is dependant on a well-known state
/// cannot retrieve it.
/// </summary>
public sealed class MissingStateException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="MissingStateException"/>.
    /// </summary>
    /// <param name="feature">
    /// The feature that depends on the missing state.
    /// </param>
    /// <param name="key">
    /// The key of the missing state.
    /// </param>
    /// <param name="kind">
    /// The state store that is missing the state.
    /// </param>
    public MissingStateException(string feature, string key, StateKind kind)
        : base(string.Format(AbstractionResources.MissingStateException_Message, feature, key))
    {
        Feature = feature;
        Key = key;
        Kind = kind;
    }

    /// <summary>
    /// Gets the feature that depends on the missing state.
    /// </summary>
    public string Feature { get; }

    /// <summary>
    /// Gets the key of the missing state.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the state store that is missing the state.
    /// </summary>
    public StateKind Kind { get; }
}
