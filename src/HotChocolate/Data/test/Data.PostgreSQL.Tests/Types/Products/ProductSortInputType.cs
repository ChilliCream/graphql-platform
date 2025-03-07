using HotChocolate.Data.Models;
using HotChocolate.Data.Sorting;
using HotChocolate.Types;

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

public partial class ProductsConnectionType
{
    static partial void Configure(IObjectTypeDescriptor<ProductsConnection> descriptor)
    {
        descriptor
            .Name(c => c.Name + "Connection")
            .DependsOn(typeof(string));
    }
}
