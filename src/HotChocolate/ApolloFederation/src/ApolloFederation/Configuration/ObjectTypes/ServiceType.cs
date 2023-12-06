using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Resolvers;
using FederationSchemaPrinter = HotChocolate.ApolloFederation.FederationSchemaPrinter;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// A new object type called _Service must be created.
/// This type must have an sdl: String! field which exposes the SDL of the service's schema.
///
/// This SDL (schema definition language) is a printed version of the service's
/// schema including the annotations of federation directives. This SDL does not
/// include the additions of the federation spec.
/// </summary>
public class ServiceType : ObjectType
{
    public ServiceType(bool isV2 = false)
    {
        IsV2 = isV2;
    }

    public bool IsV2 { get; }

    protected override void Configure(IObjectTypeDescriptor descriptor)
        => descriptor
            .Name(WellKnownTypeNames.Service)
            .Description(FederationResources.ServiceType_Description)
            .Field(WellKnownFieldNames.Sdl)
            .Type<NonNullType<StringType>>()
            .Resolve(resolver => PrintSchemaSdl(resolver, IsV2));

    private string PrintSchemaSdl(IResolverContext resolver, bool isV2)
    {
        if (isV2)
        {
            return SchemaPrinter.Print(resolver.Schema);
        }
        else
        {
            return FederationSchemaPrinter.Print(resolver.Schema);
        }
    }
}
