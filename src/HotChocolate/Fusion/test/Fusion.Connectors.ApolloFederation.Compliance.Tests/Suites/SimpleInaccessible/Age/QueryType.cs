using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInaccessible.Age;

/// <summary>
/// Root <c>Query</c> for the <c>age</c> subgraph. Exposes a shareable
/// <c>usersInAge: [User!]!</c> field returning the seeded list.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("usersInAge")
            .Type<NonNullType<ListType<NonNullType<UserType>>>>()
            .Shareable()
            .Resolve(_ => AgeData.Users);
    }
}
