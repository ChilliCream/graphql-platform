namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL type that wraps another type e.g. non-null types or list types.
/// </summary>
public interface IWrapperType : IOutputType, IInputType
{
    /// <summary>
    /// Gets the inner type.
    /// </summary>
    IType InnerType { get; }
}
