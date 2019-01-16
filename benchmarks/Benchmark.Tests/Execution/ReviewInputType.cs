using HotChocolate.Types;

namespace HotChocolate.Benchmark.Tests.Execution
{
    public class ReviewInputType
        : InputObjectType<Review>
    {
        protected override void Configure(IInputObjectTypeDescriptor<Review> descriptor)
        {
            descriptor.Name("ReviewInput");
        }
    }
}
