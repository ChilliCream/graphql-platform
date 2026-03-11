namespace HotChocolate.Types;

/// <summary>
/// Represents a non-null type.
/// </summary>
/// <remarks>
/// this is just a marker type for the fluent code-first api.
/// </remarks>
/// <typeparam name="T">
/// The inner type.
/// </typeparam>
public sealed class NonNullType<T> : FluentWrapperType where T : IType
{
    private NonNullType() { }
}
