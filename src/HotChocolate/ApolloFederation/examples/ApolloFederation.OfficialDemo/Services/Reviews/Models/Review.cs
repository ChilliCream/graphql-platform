using HotChocolate.ApolloFederation;

namespace Reviews.Models
{
    public class Review
    {
        [Key]
        public string Id { get; set; }
        public Product Product { get; set; }
        public string Body { get; set; }

        [Provides("username")]
        public User Author { get; set; }
        public string AuthorId { get; set; }
    }
}
