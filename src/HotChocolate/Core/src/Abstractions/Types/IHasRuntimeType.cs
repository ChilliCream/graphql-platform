namespace HotChocolate.Types;

/// <summary>
/// The implementor of this interface exposes the type it will have
/// at runtime when manifested in the execution engine.
/// </summary>
public interface IHasRuntimeType
{
    /// <summary>
    /// Gets the runtime type.
    /// The runtime type defines of which value the type is when it
    /// manifests in the execution engine.
    /// </summary>
    Type RuntimeType { get; }
}
