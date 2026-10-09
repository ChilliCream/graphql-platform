using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgumentConflict.B;

/// <summary>
/// Type descriptor for the <c>Category</c> type owned by the <c>b</c>
/// subgraph.
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
