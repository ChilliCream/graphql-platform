using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InputObjectIntersection.B;

/// <summary>
/// Root <c>Query</c> for the <c>b</c> subgraph. Exposes
/// <c>usersInB(filter: UsersFilter!)</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("usersInB")
            .Argument("filter", a => a.Type<NonNullType<UsersFilterType>>())
            .Type<ListType<NonNullType<UserType>>>()
            .Resolve(_ => BData.Users);
    }
}
