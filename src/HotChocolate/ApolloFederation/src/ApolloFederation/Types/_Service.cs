using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// A new object type called _Service must be created.
/// This type must have an sdl: String! field which exposes the SDL of the service's schema.
///
/// This SDL (schema definition language) is a printed version of the service's
/// schema including the annotations of federation directives. This SDL does not
/// include the additions of the federation spec.
/// </summary>
[ObjectType(ServiceType_Name)]
[GraphQLDescription(ServiceType_Description)]
// ReSharper disable once InconsistentNaming
public sealed class _Service
{
    [GraphQLName(WellKnownFieldNames.Sdl)]
    public string GetSdl(ISchema schema)
        => SchemaPrinter.Print(schema);
}
