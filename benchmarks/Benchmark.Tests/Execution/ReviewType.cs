using HotChocolate.Types;

namespace HotChocolate.Benchmark.Tests.Execution
{
    public class ReviewType
        : ObjectType<Review>
    {
        protected override void Configure(IObjectTypeDescriptor<Review> descriptor)
        {
        }
    }
}
