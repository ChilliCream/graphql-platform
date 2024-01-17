using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// # federation v1 definition
/// directive @external on FIELD_DEFINITION
///
/// # federation v2 definition
/// directive @external on OBJECT | FIELD_DEFINITION
/// </code>
///
/// The @external directive is used to mark a field as owned by another service.
/// This allows service A to use fields from service B while also knowing at runtime
/// the types of that field. All the external fields should either be referenced from the @key,
/// @requires or @provides directives field sets.
///
/// Due to the smart merging of entity types, Federation v2 no longer requires @external directive
/// on @key fields and can be safely omitted from the schema. @external directive is only required
/// on fields referenced by the @requires and @provides directive.
///
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID!
///   remoteField: String @external
///   localField: String @requires(fields: "remoteField")
/// }
/// </example>
/// </summary>
public sealed class ExternalAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
        => descriptor.External();
}
