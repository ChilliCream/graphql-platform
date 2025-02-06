using HotChocolate.Data.Models;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.Types.Products;

public sealed class ProductSortInputType : SortInputType<Product>
{
    protected override void Configure(ISortInputTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.Price);
    }
}
