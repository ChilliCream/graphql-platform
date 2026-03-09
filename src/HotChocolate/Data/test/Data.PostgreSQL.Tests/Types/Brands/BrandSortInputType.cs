using HotChocolate.Data.Models;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.Types.Brands;

public sealed class BrandSortInputType : SortInputType<Brand>
{
    protected override void Configure(ISortInputTypeDescriptor<Brand> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Name);
    }
}
