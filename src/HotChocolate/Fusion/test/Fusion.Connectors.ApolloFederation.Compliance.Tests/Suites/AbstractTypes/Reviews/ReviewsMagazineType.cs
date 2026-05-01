using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.AbstractTypes.Products;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Reviews;

public sealed class ReviewsMagazineType : ObjectType<ReviewMagazineEntity>
{
    protected override void Configure(IObjectTypeDescriptor<ReviewMagazineEntity> descriptor)
    {
        descriptor.Name("Magazine");

        descriptor
            .Implements<ReviewsProductInterfaceType>();

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field("reviewsCount")
            .Type<NonNullType<IntType>>()
            .Resolve(ctx =>
            {
                var magazine = ctx.Parent<ReviewMagazineEntity>();
                return ReviewData.ReviewsCountForProduct(magazine.Id);
            });

        descriptor
            .Field("reviewsScore")
            .Shareable()
            .Type<NonNullType<FloatType>>()
            .Resolve(ctx =>
            {
                var magazine = ctx.Parent<ReviewMagazineEntity>();
                return ReviewData.ReviewsScoreForProduct(magazine.Id);
            });

        descriptor
            .Field("reviews")
            .Type<NonNullType<ListType<NonNullType<ReviewType>>>>()
            .Resolve(ctx =>
            {
                var magazine = ctx.Parent<ReviewMagazineEntity>();
                return BuildReviewResults(magazine.Id);
            });

        descriptor
            .Field(m => m.Similar)
            .External()
            .Type<ListType<ReviewsMagazineType>>();

        descriptor
            .Field("reviewsOfSimilar")
            .Type<NonNullType<ListType<NonNullType<ReviewType>>>>()
            .Requires("similar { id }")
            .Resolve(ctx =>
            {
                var magazine = ctx.Parent<ReviewMagazineEntity>();

                if (magazine.Similar is null)
                {
                    return new List<ReviewResult>();
                }

                var similarIds = magazine.Similar.Select(s => s.Id).ToList();

                return ProductData.Magazines
                    .Where(m => similarIds.Contains(m.Id))
                    .SelectMany(m => ReviewData.ReviewsForProduct(m.Id))
                    .Select(r => BuildReviewResult(r))
                    .ToList();
            });
    }

    private static ReviewMagazineEntity ResolveById(string id)
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
