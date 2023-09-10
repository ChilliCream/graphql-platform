#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Represents a middleware or result converter configuration.
/// </summary>
public interface IMiddlewareDefinition
{
    /// <summary>
    /// Defines if the middleware or result converters is repeatable and
    /// the same middleware is allowed to be occur multiple times.
    /// </summary>
    public bool IsRepeatable { get; }

    /// <summary>
    /// The key is optional and is used to identify a middleware.
    /// </summary>
    public string? Key { get; }
}
