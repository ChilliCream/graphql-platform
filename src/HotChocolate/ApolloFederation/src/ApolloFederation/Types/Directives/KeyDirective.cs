using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @key(fields: _FieldSet!) repeatable on OBJECT | INTERFACE
/// </code>
///
/// The @key directive is used to indicate a combination of fields that can be used to uniquely
/// identify and fetch an object or interface. The specified field set can represent single field (e.g. "id"),
/// multiple fields (e.g. "id name") or nested selection sets (e.g. "id user { name }"). Multiple keys can
/// be specified on a target type.
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID!
///   field: String
/// }
/// </example>
/// </summary>
[DirectiveType(DirectiveLocation.Object | DirectiveLocation.Interface, IsRepeatable = true)]
[GraphQLDescription(Descriptions.KeyDirective_Description)]
[KeyLegacySupport]
public sealed class KeyDirective(string fieldSet, bool resolvable = true)
{
    [FieldSet]
    public string FieldSet { get; } = fieldSet;
    
    [GraphQLType<BooleanType>]
    [DefaultValue(true)]
    public bool Resolvable { get; } = resolvable;
}

internal sealed class KeyLegacySupportAttribute : DirectiveTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context, 
        IDirectiveTypeDescriptor descriptor, 
        Type type)
    {
        if (descriptor.GetFederationVersion() == FederationVersion.Federation10)
        {
            var desc = (IDirectiveTypeDescriptor<KeyDirective>)descriptor;
            desc.BindArgumentsExplicitly();
            desc.Argument(t => t.FieldSet);
        }
    }
}
