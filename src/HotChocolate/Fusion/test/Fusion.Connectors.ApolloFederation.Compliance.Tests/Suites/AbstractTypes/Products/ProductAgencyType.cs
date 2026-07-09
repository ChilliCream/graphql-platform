using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Products;

public sealed class ProductAgencyType : ObjectType<AgencyPublisher>
{
    protected override void Configure(IObjectTypeDescriptor<AgencyPublisher> descriptor)
    {
        descriptor.Name("Agency");
        descriptor.Field(a => a.Id).Shareable().Type<NonNullType<IdType>>();
    }
}
