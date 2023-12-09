using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// directive @tag(name: String!) repeatable on FIELD_DEFINITION
///  | OBJECT
///  | INTERFACE
///  | UNION
///  | ENUM
///  | ENUM_VALUE
///  | SCALAR
///  | INPUT_OBJECT
///  | INPUT_FIELD_DEFINITION
///  | ARGUMENT_DEFINITION
/// </code>
///
/// The @tag directive allows users to annotate fields and types with additional metadata information.
/// Tagging is commonly used for creating variants of the supergraph using contracts.
///
/// <example>
/// type Foo @tag(name: "internal") {
///   id: ID!
///   name: String
/// }
/// </example>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Enum |
    AttributeTargets.Field |
    AttributeTargets.Interface |
    AttributeTargets.Method |
    AttributeTargets.Parameter |
    AttributeTargets.Property |
    AttributeTargets.Struct,
    AllowMultiple = true)]
public sealed class ApolloTagAttribute : DescriptorAttribute
{
    /// <summary>
    /// Initializes new instance of <see cref="ApolloTagAttribute"/>
    /// </summary>
    /// <param name="name">
    /// Tag metadata value
    /// </param>
    public ApolloTagAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Retrieves tag metadata value
    /// </summary>
    public string Name { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IEnumTypeDescriptor enumDescriptor:
            {
                enumDescriptor.ApolloTag(Name);
                break;
            }
            case IEnumValueDescriptor enumValueDescriptor:
            {
                enumValueDescriptor.ApolloTag(Name);
                break;
            }
            case IInputObjectTypeDescriptor inputObjectTypeDescriptor:
            {
                inputObjectTypeDescriptor.ApolloTag(Name);
                break;
            }
            case IInputFieldDescriptor inputFieldDescriptor:
            {
                inputFieldDescriptor.ApolloTag(Name);
                break;
            }
            case IInterfaceTypeDescriptor interfaceTypeDescriptor:
            {
                interfaceTypeDescriptor.ApolloTag(Name);
                break;
            }
            case IObjectFieldDescriptor objectFieldDescriptor:
            {
                objectFieldDescriptor.ApolloTag(Name);
                break;
            }
            case IUnionTypeDescriptor unionTypeDescriptor:
            {
                unionTypeDescriptor.ApolloTag(Name);
                break;
            }
        }
    }
}
