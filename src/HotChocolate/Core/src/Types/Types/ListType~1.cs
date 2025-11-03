namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL list type.
/// </summary>
/// <remarks>
/// This is just a marker type for the fluent code-first api.
/// </remarks>
/// <typeparam name="T">
/// The inner type.
/// </typeparam>
public sealed class ListType<T> : FluentWrapperType where T : IType
{
    private ListType() { }
}
