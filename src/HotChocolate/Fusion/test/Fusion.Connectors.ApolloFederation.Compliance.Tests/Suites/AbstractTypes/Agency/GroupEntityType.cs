using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Agency;

public sealed class GroupEntityType : ObjectType<GroupEntity>
{
    protected override void Configure(IObjectTypeDescriptor<GroupEntity> descriptor)
    {
        descriptor.Name("Group");

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(g => g.Id).Type<NonNullType<IdType>>();
        descriptor.Field(g => g.Name).Type<StringType>();
        descriptor.Field(g => g.Email).Type<StringType>();
    }

    private static GroupEntity ResolveById(string id)
        => AgencyData.ResolveGroup(id);
}
