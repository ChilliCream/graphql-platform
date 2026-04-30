using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Reviews;

/// <summary>
/// Apollo Federation descriptor for the <c>Review</c> entity owned by the
/// <c>reviews</c> subgraph. Mirrors the audit Schema Definition Language
/// (SDL) <c>type Review @key(fields: "id")</c> with fields
/// <c>id: ID!</c>, <c>body: String</c>,
/// <c>author: User @provides(fields: "username")</c>, and <c>product: Product</c>.
/// The <c>author</c> resolver inlines <c>username</c> alongside the
/// returned <see cref="User"/> so the gateway can satisfy the provides
/// selection without dispatching a fresh entity call to the
/// <c>accounts</c> subgraph.
/// </summary>
public sealed class ReviewType : ObjectType<Review>
{
    protected override void Configure(IObjectTypeDescriptor<Review> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(r => r.Id).Type<NonNullType<IdType>>();
        descriptor.Field(r => r.Body).Type<StringType>();

        descriptor
            .Field("author")
            .Type<UserType>()
            .Provides("username")
            .Resolve(ctx =>
            {
                var review = ctx.Parent<Review>();
                if (review.AuthorId is not { Length: > 0 } authorId)
                {
                    return null;
                }

                var username = ReviewsData.UsernameById.TryGetValue(authorId, out var u) ? u : null;
                return new User { Id = authorId, Username = username };
            });

        descriptor
            .Field("product")
            .Type<ProductType>()
            .Resolve(ctx =>
            {
                var review = ctx.Parent<Review>();
                if (review.ProductUpc is not { Length: > 0 } upc)
                {
                    return null;
                }

                return new Product { Upc = upc };
            });
    }

    private static Review? ResolveById(string id)
        => ReviewsData.ById.TryGetValue(id, out var review) ? review : null;
}
