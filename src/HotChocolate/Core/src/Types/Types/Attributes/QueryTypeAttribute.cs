using System;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// Marks a class as an extension of the query operation type.
/// </summary>
public sealed class QueryTypeAttribute
    : ObjectTypeDescriptorAttribute
    , ITypeAttribute
{
    /// <summary>
    /// Defines if this attribute is inherited. The default is <c>false</c>.
    /// </summary>
    public bool Inherited { get; set; }

    TypeKind ITypeAttribute.Kind => TypeKind.Object;

    bool ITypeAttribute.IsTypeExtension => true;

    public override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
        => descriptor.Name(OperationTypeNames.Query);
}
