using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgumentConflict.A;

/// <summary>
/// Type descriptor for the externally referenced <c>Category</c> type
/// in the <c>a</c> subgraph.
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
