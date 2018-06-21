namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class Mutation
    {
        public Review CreateReview(Episode episode, Review review)
        {
            return review;
        }
    }
}
