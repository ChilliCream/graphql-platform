using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// The @key directive is used to indicate a combination of fields that
    /// can be used to uniquely identify and fetch an object or interface.
    /// </summary>
    public sealed class KeyDirectiveType
        : DirectiveType
    {
        public const string ContextDataMarkerName = "HotChocolate.ApolloFederation.Key";
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name(TypeNames.Key)
                .Description(FederationResources.KeyDirective_Description)
                .Location(DirectiveLocation.Object | DirectiveLocation.Interface)
                .FieldsArgument();
        }
    }
}
