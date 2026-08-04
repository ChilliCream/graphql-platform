using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Products;

public sealed class ProductInterfaceType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Product");

        descriptor.Field("id").Type<NonNullType<IdType>>();
        descriptor.Field("sku").Type<StringType>();
        descriptor.Field("dimensions").Type<ProductDimensionType>();
        descriptor.Field("createdBy").Type<ProductUserType>();
        descriptor.Field("hidden").Type<BooleanType>().Inaccessible();
    }
}
