using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @shareable repeatable on FIELD_DEFINITION | OBJECT
/// </code>
///
/// The @shareable directive indicates that given object and/or field can be resolved by multiple subgraphs.
/// If an object is marked as @shareable then all its fields are automatically shareable without the need
/// for explicitly marking them with @shareable directive. All fields referenced from @key directive are
/// automatically shareable as well.
///
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID!                           # shareable because id is a key field
///   name: String                      # non-shareable
///   description: String @shareable    # shareable
/// }
///
/// type Bar @shareable {
///   description: String               # shareable because User is marked shareable
///   name: String                      # shareable because User is marked shareable
/// }
/// </example>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Method
    | AttributeTargets.Property)]
public sealed class ShareableAttribute : DescriptorAttribute
{
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IObjectTypeDescriptor objectTypeDescriptor:
            {
                objectTypeDescriptor.Shareable();
                break;
            }
            case IObjectFieldDescriptor objectFieldDescriptor:
            {
                objectFieldDescriptor.Shareable();
                break;
            }
        }
    }
}
