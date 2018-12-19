using HotChocolate.Types;

namespace HotChocolate.Integration.StarWarsCodeFirst
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
