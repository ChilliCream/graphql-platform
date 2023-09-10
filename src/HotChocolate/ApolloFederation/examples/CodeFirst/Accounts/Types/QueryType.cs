using HotChocolate.Types;

namespace Accounts;

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Name("Query")
            .Field("me")
            .Type<NonNullType<UserType>>()
            .Resolve(ctx => ctx.Service<UserRepository>().GetUserByIdAsync("1"));
    }
}
