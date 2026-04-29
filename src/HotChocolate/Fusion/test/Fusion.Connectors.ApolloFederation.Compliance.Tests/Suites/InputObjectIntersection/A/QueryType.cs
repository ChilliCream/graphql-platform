using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InputObjectIntersection.A;

/// <summary>
/// Root <c>Query</c> for the <c>a</c> subgraph. Exposes
/// <c>usersInA(filter: UsersFilter!)</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("usersInA")
            .Argument("filter", a => a.Type<NonNullType<UsersFilterType>>())
            .Type<ListType<NonNullType<UserType>>>()
            .Resolve(_ => AData.Users);
    }
}
