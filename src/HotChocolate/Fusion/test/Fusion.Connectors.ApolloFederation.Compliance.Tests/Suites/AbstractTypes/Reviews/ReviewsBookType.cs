using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.AbstractTypes.Products;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Reviews;

public sealed class ReviewsBookType : ObjectType<ReviewBookEntity>
{
    protected override void Configure(IObjectTypeDescriptor<ReviewBookEntity> descriptor)
    {
        descriptor.Name("Book");

        descriptor
            .Implements<ReviewsProductInterfaceType>();

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field("reviewsCount")
            .Type<NonNullType<IntType>>()
            .Resolve(ctx =>
            {
                var book = ctx.Parent<ReviewBookEntity>();
                return ReviewData.ReviewsCountForProduct(book.Id);
            });

        descriptor
            .Field("reviewsScore")
            .Shareable()
            .Type<NonNullType<FloatType>>()
            .Resolve(ctx =>
            {
                var book = ctx.Parent<ReviewBookEntity>();
                return ReviewData.ReviewsScoreForProduct(book.Id);
            });

        descriptor
            .Field("reviews")
            .Type<NonNullType<ListType<NonNullType<ReviewType>>>>()
            .Resolve(ctx =>
            {
                var book = ctx.Parent<ReviewBookEntity>();
                return BuildReviewResults(book.Id);
            });

        descriptor
            .Field(b => b.Similar)
            .External()
            .Type<ListType<ReviewsBookType>>();

        descriptor
            .Field("reviewsOfSimilar")
            .Type<NonNullType<ListType<NonNullType<ReviewType>>>>()
            .Requires("similar { id }")
            .Resolve(ctx =>
            {
                var book = ctx.Parent<ReviewBookEntity>();

                if (book.Similar is null)
                {
                    return new List<ReviewResult>();
                }

                var similarIds = book.Similar.Select(s => s.Id).ToList();

                return ProductData.Books
                    .Where(b => similarIds.Contains(b.Id))
                    .SelectMany(b => ReviewData.ReviewsForProduct(b.Id))
                    .Select(r => BuildReviewResult(r))
                    .ToList();
            });
    }

    private static ReviewBookEntity ResolveById(string id)
        => new() { Id = id };

    private static List<ReviewResult> BuildReviewResults(string productId)
        => ReviewData.ReviewsForProduct(productId)
            .Select(r => BuildReviewResult(r))
            .ToList();

    private static ReviewResult BuildReviewResult(ReviewEntity r)
    {
        var typeName = ReviewData.GetProductTypeName(r.ProductId);
        IProductRef? productRef = typeName switch
        {
            "Book" => new BookRef { Id = r.ProductId },
            "Magazine" => new MagazineRef { Id = r.ProductId },
            _ => null
        };

        return new ReviewResult { Id = r.Id, Body = r.Body, Product = productRef };
    }
}
