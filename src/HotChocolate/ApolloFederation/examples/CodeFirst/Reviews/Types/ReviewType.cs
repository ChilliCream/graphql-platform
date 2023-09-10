using HotChocolate.Types;

namespace Reviews;

public class ReviewType : ObjectType<Review>
{
    protected override void Configure(IObjectTypeDescriptor<Review> descriptor)
    {
        descriptor
            .Key("id");

        descriptor
            .Field(t => t.Product)
            .Type<NonNullType<ProductType>>();

        descriptor
            .Field("author")
            .Provides("username")
            .Type<NonNullType<UserType>>()
            .Resolve(async ctx => 
            {
                var repository = ctx.Service<UserRepository>();
                var authorId = ctx.Parent<Review>().AuthorId;
                return await repository.GetUserById(authorId);
            });
    }
}
