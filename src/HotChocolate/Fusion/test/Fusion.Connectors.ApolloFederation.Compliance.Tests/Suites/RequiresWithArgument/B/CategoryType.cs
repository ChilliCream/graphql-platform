using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.B;

/// <summary>
/// Type descriptor for the <c>Category</c> type owned by the <c>b</c>
/// subgraph. Exposes <c>averagePrice(currency: String!): Int</c>.
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
