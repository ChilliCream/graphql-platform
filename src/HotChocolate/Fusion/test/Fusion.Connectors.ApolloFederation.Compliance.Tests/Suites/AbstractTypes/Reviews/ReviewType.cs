using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Reviews;

public sealed class ReviewType : ObjectType<ReviewResult>
{
    protected override void Configure(IObjectTypeDescriptor<ReviewResult> descriptor)
    {
        descriptor.Name("Review");
        descriptor.Field(r => r.Id).Type<NonNullType<IntType>>();
        descriptor.Field(r => r.Body).Type<NonNullType<StringType>>();
        descriptor.Field(r => r.Product).Type<ReviewsProductInterfaceType>();
    }
}
