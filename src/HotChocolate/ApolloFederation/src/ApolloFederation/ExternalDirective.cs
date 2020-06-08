using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// The @external directive is used to mark a field as owned by another service. 
    /// This allows service A to use fields from service B while also knowing at 
    /// runtime the types of that field.
    /// </summary>
    public sealed class ExternalDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name(TypeNames.External)
                .Description(FederationResources.ExternalDirective_Description)
                .Location(DirectiveLocation.FieldDefinition);
        }
    }
}
