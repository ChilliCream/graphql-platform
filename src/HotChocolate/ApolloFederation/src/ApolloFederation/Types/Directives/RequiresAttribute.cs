using System.Reflection;
using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// # federation v1 definition
/// directive @requires(fields: _FieldSet!) on FIELD_DEFINITION
///
/// # federation v2 definition
/// directive @requires(fields: FieldSet!) on FIELD_DEFINITION
/// </code>
///
/// The @requires directive is used to specify external (provided by other subgraphs)
/// entity fields that are needed to resolve target field. It is used to develop a query plan where
/// the required fields may not be needed by the client, but the service may need additional
/// information from other subgraphs. Required fields specified in the directive field set should
/// correspond to a valid field on the underlying GraphQL interface/object and should be instrumented
/// with @external directive.
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID!
///   # this field will be resolved from other subgraph
///   remote: String @external
///   local: String @requires(fields: "remote")
/// }
/// </example>
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="RequiresAttribute"/>.
/// </remarks>
/// <param name="fieldSet">
/// The <paramref name="fieldSet"/> describes which fields may
/// not be needed by the client, but are required by
/// this service as additional information from other services.
/// Grammatically, a field set is a selection set minus the braces.
/// </param>
public sealed class RequiresAttribute(string fieldSet) : ObjectFieldDescriptorAttribute
{
    /// <summary>
    /// Gets the fieldset which describes fields that may not be needed by the client,
    /// but are required by this service as additional information from other services.
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
            throw Requires_FieldSet_CannotBeEmpty(member);
        }

        descriptor.Requires(FieldSet);
    }
}
