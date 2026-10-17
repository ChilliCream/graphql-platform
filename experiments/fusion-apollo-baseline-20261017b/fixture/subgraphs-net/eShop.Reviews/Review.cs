using HotChocolate;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace eShop.Reviews;

[Key("id")]
public sealed class Review
{
    [GraphQLType(typeof(IdType))]
    public required string Id { get; init; }

    public required string Body { get; init; }

    public required string AuthorId { get; init; }

    public required string ProductUpc { get; init; }

    public User? GetAuthor()
        => User.Users.TryGetValue(AuthorId, out var user) ? user : null;

    public Product? GetProduct()
        => Product.Products.TryGetValue(ProductUpc, out var product) ? product : null;

    [ReferenceResolver]
    public static Review? ResolveReference(string id)
        => ReviewRepository.GetById(id);
}
