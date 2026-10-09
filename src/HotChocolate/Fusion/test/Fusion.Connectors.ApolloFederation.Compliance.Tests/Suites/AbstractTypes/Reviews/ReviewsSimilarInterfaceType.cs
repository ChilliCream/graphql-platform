using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Reviews;

public sealed class ReviewsSimilarInterfaceType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Similar");
        descriptor.Field("similar").Type<ListType<ReviewsProductInterfaceType>>();
    }
}
