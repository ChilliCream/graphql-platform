using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

internal interface IHasTypeDefinition
{
    /// <summary>
    /// Gets the inner type definition if it is still available.
    /// </summary>
    ITypeDefinition? Definition { get; }
}
