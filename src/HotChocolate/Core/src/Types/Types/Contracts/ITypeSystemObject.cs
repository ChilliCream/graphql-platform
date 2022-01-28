namespace HotChocolate.Types;

/// <summary>
/// Types and directives are type system objects.
/// </summary>
public interface ITypeSystemObject
    : IHasName
    , IHasDescription
    , IHasReadOnlyContextData
    , IHasScope
    , ITypeSystemMember
{
}
