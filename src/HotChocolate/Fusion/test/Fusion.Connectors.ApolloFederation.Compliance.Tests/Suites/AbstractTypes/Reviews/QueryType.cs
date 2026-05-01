using HotChocolate.Fusion.Suites.AbstractTypes.Products;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Reviews;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("review")
            .Argument("id", a => a.Type<NonNullType<IntType>>())
            .Type<ReviewType>()
            .Resolve(ctx =>
            {
                var id = ctx.ArgumentValue<int>("id");

                if (!ReviewData.ReviewsById.TryGetValue(id, out var review))
                {
                    return null;
                }

                var typeName = ReviewData.GetProductTypeName(review.ProductId);
                IProductRef? productRef = typeName switch
                {
                    "Book" => new BookRef { Id = review.ProductId },
                    "Magazine" => new MagazineRef { Id = review.ProductId },
                    _ => null
                };

                return new ReviewResult
                {
                    Id = review.Id,
                    Body = review.Body,
                    Product = productRef
                };
            });
    }
}
