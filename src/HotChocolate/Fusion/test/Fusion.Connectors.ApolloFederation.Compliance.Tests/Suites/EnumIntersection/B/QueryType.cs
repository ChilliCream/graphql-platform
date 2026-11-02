using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.EnumIntersection.B;

/// <summary>
/// Root <c>Query</c> for the <c>b</c> subgraph. Exposes
/// <c>usersByType(type: UserType!): [User!]</c> and
/// <c>usersB: [User!]</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("usersByType")
            .Argument("type", a => a.Type<NonNullType<UserTypeType>>())
            .Type<ListType<NonNullType<UserGraphType>>>()
            .Resolve(ctx =>
            {
                var requested = ctx.ArgumentValue<UserTypeEnum>("type");
                return BData.Users.Where(u => u.Type == requested).ToArray();
            });

        descriptor
            .Field("usersB")
            .Type<ListType<NonNullType<UserGraphType>>>()
            .Resolve(_ => BData.Users);
    }
}
