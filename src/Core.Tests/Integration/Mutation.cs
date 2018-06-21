using HotChocolate.Types;

namespace HotChocolate.Integration
{
    public class Mutation
    {
        public Review CreateReview(Episode episode, Review review)
        {
            return review;
        }
    }

    public class MutationType
        : ObjectType<Mutation>
    {
        protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
        {
            descriptor.Field(t => t.CreateReview(default, default))
                .Type<NonNullType<ReviewType>>()
                .Argument("review", a => a.Type<NonNullType<ReviewInputType>>());
        }
    }

    public class ReviewInputType
        : InputObjectType<Review>
    {
        protected override void Configure(IInputObjectTypeDescriptor<Review> descriptor)
        {
            descriptor.Name("ReviewInput");
        }
    }

    public class ReviewType
        : ObjectType<Review>
    {
        protected override void Configure(IObjectTypeDescriptor<Review> descriptor)
        {
        }
    }

    public class Review
    {
        public int Stars { get; set; }
        public string Commentary { get; set; }
    }
}
