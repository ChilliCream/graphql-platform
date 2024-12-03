using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// The @tag directive is used to apply arbitrary string
/// metadata to a schema location. Custom tooling can use
/// this metadata during any step of the schema delivery flow,
/// including composition, static analysis, and documentation.
///
/// <code>
/// interface Book {
///   id: ID! @tag(name: "your-value")
///   title: String!
///   author: String!
/// }
/// </code>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Interface |
    AttributeTargets.Enum |
    AttributeTargets.Property |
    AttributeTargets.Method |
    AttributeTargets.Field |
    AttributeTargets.Parameter,
    AllowMultiple = true)]
public sealed class TagAttribute : DescriptorAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="TagAttribute"/>.
    /// </summary>
    /// <param name="name">
    /// The <paramref name="name"/> applies arbitrary string metadata
    /// to a schema location
    /// </param>
    public TagAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name associated with this tag
    /// </summary>
    public string Name { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IObjectTypeDescriptor desc:
                desc.Tag(Name);
                break;

            case IInterfaceTypeDescriptor desc:
                desc.Tag(Name);
                break;

            case IUnionTypeDescriptor desc:
                desc.Tag(Name);
                break;

            case IInputObjectTypeDescriptor desc:
                desc.Tag(Name);
                break;

            case IEnumTypeDescriptor desc:
                desc.Tag(Name);
                break;

            case IObjectFieldDescriptor desc:
                desc.Tag(Name);
                break;

            case IInterfaceFieldDescriptor desc:
                desc.Tag(Name);
                break;

            case IInputFieldDescriptor desc:
                desc.Tag(Name);
                break;

            case IArgumentDescriptor desc:
                desc.Tag(Name);
                break;

            case IDirectiveArgumentDescriptor desc:
                desc.Tag(Name);
                break;

            case IEnumValueDescriptor desc:
                desc.Tag(Name);
                break;

            case ISchemaTypeDescriptor desc:
                desc.Tag(Name);
                break;

            default:
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage(TypeResources.TagDirective_Descriptor_NotSupported)
                        .SetExtension("member", element)
                        .SetExtension("descriptor", descriptor)
                        .Build());
        }
    }
}
