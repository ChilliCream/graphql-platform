using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;

namespace HotChocolate.ApolloFederation;

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
public sealed class KeyDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor
            .Name(WellKnownTypeNames.Key)
            .Description(FederationResources.KeyDirective_Description)
            .Location(DirectiveLocation.Object | DirectiveLocation.Interface)
            .Repeatable();
        
        descriptor
            .FieldsArgument();
            
        if(descriptor.GetFederationVersion() > FederationVersion.Federation10)
        {
            descriptor
                .Argument(WellKnownArgumentNames.Resolvable)
                .Type<BooleanType>()
                .DefaultValue(true);
        }
        descriptor
            .Argument(WellKnownArgumentNames.Resolvable)
            .Type<BooleanType>()
            .DefaultValue(true);
        
    }
}
