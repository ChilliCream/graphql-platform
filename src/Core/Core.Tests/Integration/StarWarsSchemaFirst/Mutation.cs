namespace HotChocolate.Integration.StarWarsSchemaFirst
{
    public class Mutation
    {
        public Review CreateReview(Episode episode, Review review)
        {
            return review;
        }
    }
}
