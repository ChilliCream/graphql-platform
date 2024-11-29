using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// Marks a class as an extension of the mutation operation type.
/// </summary>
public sealed class MutationTypeAttribute
    : ObjectTypeDescriptorAttribute
    , ITypeAttribute
{
    /// <summary>
    /// Defines if this attribute is inherited. The default is <c>false</c>.
    /// </summary>
    public bool Inherited { get; set; }

    /// <summary>
    /// Defines that static members are included.
    /// </summary>
    public bool IncludeStaticMembers { get; set; }

    TypeKind ITypeAttribute.Kind => TypeKind.Object;

    bool ITypeAttribute.IsTypeExtension => true;

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
    {
        descriptor.Name(OperationTypeNames.Mutation);

        var definition = descriptor.Extend().Definition;
        definition.Fields.BindingBehavior = BindingBehavior.Implicit;

        if (IncludeStaticMembers)
        {
            definition.FieldBindingFlags = FieldBindingFlags.Instance | FieldBindingFlags.Static;
        }
    }
}
