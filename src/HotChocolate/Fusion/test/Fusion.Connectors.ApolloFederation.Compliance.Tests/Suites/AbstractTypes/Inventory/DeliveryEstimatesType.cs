using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Inventory;

public sealed class DeliveryEstimatesType : ObjectType<DeliveryEstimates>
{
    protected override void Configure(IObjectTypeDescriptor<DeliveryEstimates> descriptor)
    {
        descriptor.Field(d => d.EstimatedDelivery).Type<StringType>();
        descriptor.Field(d => d.FastestDelivery).Type<StringType>();
    }
}
