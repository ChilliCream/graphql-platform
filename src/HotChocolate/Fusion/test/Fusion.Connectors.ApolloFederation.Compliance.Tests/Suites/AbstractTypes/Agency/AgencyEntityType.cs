using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Agency;

public sealed class AgencyEntityType : ObjectType<AgencyEntity>
{
    protected override void Configure(IObjectTypeDescriptor<AgencyEntity> descriptor)
    {
        descriptor.Name("Agency");

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.CompanyName).Type<StringType>();
        descriptor.Field(a => a.Email).Type<EmailType>();
    }

    private static AgencyEntity? ResolveById(string id)
        => AgencyData.AgenciesById.TryGetValue(id, out var agency) ? agency : null;
}
