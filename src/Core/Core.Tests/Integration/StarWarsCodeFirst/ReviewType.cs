using HotChocolate.Types;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class ReviewType
        : ObjectType<Review>
    {
        protected override void Configure(IObjectTypeDescriptor<Review> descriptor)
        {
        }
    }
}
