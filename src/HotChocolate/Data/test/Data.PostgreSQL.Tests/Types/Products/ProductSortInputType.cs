using HotChocolate.Data.Models;
using HotChocolate.Data.Sorting;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Pagination;

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
