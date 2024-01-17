using HotChocolate.Language;
using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.FederationVersionUrls;
using static HotChocolate.ApolloFederation.Properties.FederationResources;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

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
[Package(Federation20)]
[DirectiveType(
    KeyDirective_Name, 
    DirectiveLocation.Object | 
    DirectiveLocation.Interface, 
    IsRepeatable = true)]
[GraphQLDescription(KeyDirective_Description)]
[KeyLegacySupport]
public sealed class KeyDirective
{
    public KeyDirective(string fieldSet, bool resolvable = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(fieldSet);
        FieldSet = FieldSetType.ParseSelectionSet(fieldSet);
        Resolvable = resolvable;
    }
    
    public KeyDirective(SelectionSetNode fieldSet, bool resolvable = true)
    {
        ArgumentNullException.ThrowIfNull(fieldSet);
        FieldSet = fieldSet;
        Resolvable = resolvable;
    }
    
    [FieldSet]
    public SelectionSetNode FieldSet { get; }
    
    [GraphQLType<BooleanType>]
    [DefaultValue(true)]
    public bool Resolvable { get; }
}
