using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Magazines;

public sealed class MagazineType : ObjectType<Magazine>
{
    protected override void Configure(IObjectTypeDescriptor<Magazine> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();
        descriptor.Field(m => m.Title).Type<StringType>();
    }

    private static Magazine? ResolveById(string id)
        => MagazineData.MagazinesById.TryGetValue(id, out var magazine) ? magazine : null;
}
