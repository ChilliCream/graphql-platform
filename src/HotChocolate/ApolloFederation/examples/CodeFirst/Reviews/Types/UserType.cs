using HotChocolate.Types;

namespace Reviews;

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .ExtendServiceType()
            .Key("id")
            .ResolveReferenceWith(t => GetUserByIdAsync(default!, default!));

        descriptor
            .Field(t => t.Id)
            .External();

        descriptor
            .Field(t => t.Username)
            .External();

        descriptor
            .Field("reviews")
            .Type<NonNullType<ListType<NonNullType<ReviewType>>>>()
            .Resolve(async ctx => 
            {
                var repository = ctx.Service<ReviewRepository>();
                var id = ctx.Parent<User>().Id;
                return await repository.GetByUserIdAsync(id);
            });
    }

    private static Task<User> GetUserByIdAsync(
        string id,
        UserRepository repository)
        => repository.GetUserById(id);
}
