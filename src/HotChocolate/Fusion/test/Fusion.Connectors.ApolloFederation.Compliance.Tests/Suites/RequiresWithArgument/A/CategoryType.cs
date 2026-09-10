using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.A;

/// <summary>
/// Type descriptor for the externally referenced <c>Category</c> type
/// in the <c>a</c> subgraph. Declares <c>averagePrice(currency: String!): Int</c>
/// to match the owning subgraph's schema shape.
/// </summary>
public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor
            .Field("averagePrice")
            .Type<IntType>()
            .Argument("currency", a => a.Type<NonNullType<StringType>>())
            .Resolve(ctx => ctx.Parent<Category>().AveragePrice);
    }
}
