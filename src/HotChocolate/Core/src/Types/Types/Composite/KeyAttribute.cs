using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @key directive is used to designate an entity’s unique key,
/// which identifies how to uniquely reference an instance of
/// an entity across different source schemas.
/// </para>
/// <para>
/// directive @key(fields: FieldSelectionSet!) on OBJECT | INTERFACE
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--key"/>
/// </para>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Interface
    | AttributeTargets.Struct,
    AllowMultiple = true)]
public class KeyAttribute : DescriptorAttribute
{
    public KeyAttribute(string fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Fields = fields;
    }

    public string Fields { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IObjectTypeDescriptor objectTypeDescriptor:
                objectTypeDescriptor.Key(Fields);
                break;

            case IInterfaceTypeDescriptor interfaceTypeDescriptor:
                interfaceTypeDescriptor.Key(Fields);
                break;

            default:
                throw new NotSupportedException(
                    $"The {descriptor.GetType().Name} descriptor is not supported.");
        }
    }
}
