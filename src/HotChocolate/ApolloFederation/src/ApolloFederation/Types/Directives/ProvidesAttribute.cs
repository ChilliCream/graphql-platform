using System.Reflection;
using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @provides(fields: _FieldSet!) on FIELD_DEFINITION
/// </code>
///
/// The @provides directive is a router optimization hint specifying field set that
/// can be resolved locally at the given subgraph through this particular query path. This
/// allows you to expose only a subset of fields from the underlying entity type to be selectable
/// from the federated schema without the need to call other subgraphs. Provided fields specified
/// in the directive field set should correspond to a valid field on the underlying GraphQL
/// interface/object type. @provides directive can only be used on fields returning entities.
/// <example>
/// type Foo @key(fields: "id") {
///     id: ID!
///     # implies name field can be resolved locally
///     bar: Bar @provides(fields: "name")
///     # name fields are external
///     # so will be fetched from other subgraphs
///     bars: [Bar]
/// }
///
/// type Bar @key(fields: "id") {
///     id: ID!
///     name: String @external
/// }
/// </example>
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ProvidesAttribute"/>.
/// </remarks>
/// <param name="fieldSet">
/// Gets the fields that is guaranteed to be selectable by the gateway.
/// Grammatically, a field set is a selection set minus the braces.
/// </param>
public sealed class ProvidesAttribute(string fieldSet) : ObjectFieldDescriptorAttribute
{
    /// <summary>
    /// Gets the fields that are guaranteed to be selectable by the gateway.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </summary>
    public string FieldSet { get; } = fieldSet;

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        if (FieldSet is null)
        {
            throw Provides_FieldSet_CannotBeEmpty(member);
        }

        descriptor.Provides(FieldSet);
    }
}
