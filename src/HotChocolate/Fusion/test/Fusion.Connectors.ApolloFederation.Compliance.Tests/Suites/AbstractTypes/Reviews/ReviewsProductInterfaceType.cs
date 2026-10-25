using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Reviews;

public sealed class ReviewsProductInterfaceType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Product");

        descriptor.Field("id").Type<NonNullType<IdType>>();
        descriptor.Field("reviewsCount").Type<NonNullType<IntType>>();
        descriptor.Field("reviewsScore").Type<NonNullType<FloatType>>();
        descriptor.Field("reviews").Type<NonNullType<ListType<NonNullType<ReviewType>>>>();
    }
}
