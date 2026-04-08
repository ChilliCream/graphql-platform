using Mocha.Features;

namespace Mocha;

/// <summary>
/// A pooled feature that caches the deserialized message object to avoid redundant deserialization
/// within a single receive pipeline execution.
/// </summary>
public sealed class MessageParsingFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the cached deserialized message object.
    /// </summary>
    public object? Message { get; set; }

    public void Initialize(object state)
    {
        Message = null;
    }

    public void Reset()
    {
        Message = null;
    }
}
