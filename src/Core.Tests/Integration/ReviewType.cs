using HotChocolate.Types;

namespace HotChocolate.Integration
{
    public class ReviewType
        : ObjectType<Review>
    {
        protected override void Configure(IObjectTypeDescriptor<Review> descriptor)
        {
        }
    }
}
