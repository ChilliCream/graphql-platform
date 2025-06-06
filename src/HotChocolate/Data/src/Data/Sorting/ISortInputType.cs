using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Specifies a sort input type.
/// </summary>
public interface ISortInputType : IInputObjectTypeDefinition
{
    IExtendedType EntityType { get; }
}
