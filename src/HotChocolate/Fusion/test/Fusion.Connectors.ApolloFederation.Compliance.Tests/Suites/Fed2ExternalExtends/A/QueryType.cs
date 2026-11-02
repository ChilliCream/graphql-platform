using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Fed2ExternalExtends.A;

/// <summary>
/// Root <c>Query</c> type for the <c>a</c> subgraph. Exposes
/// <c>randomUser: User</c> and
/// <c>providedRandomUser: User @provides(fields: "name")</c>.
/// The <c>providedRandomUser</c> path returns a user with the
/// otherwise external <c>name</c> field already populated so the gateway
/// can read <c>name</c> without dispatching a separate entity call to
/// subgraph <c>b</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("randomUser")
            .Type<UserType>()
            .Resolve(_ => new User { Id = AData.Users[0].Id, Rid = AData.Users[0].Rid });

        descriptor
            .Field("providedRandomUser")
            .Type<UserType>()
            .Provides("name")
            .Resolve(_ => new User
            {
                Id = AData.Users[0].Id,
                Rid = AData.Users[0].Rid,
                Name = AData.Users[0].Name
            });
    }
}
