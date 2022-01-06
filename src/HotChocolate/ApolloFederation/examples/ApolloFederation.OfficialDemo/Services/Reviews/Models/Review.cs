using HotChocolate.ApolloFederation;

namespace Reviews.Models
{
    public class Review
    {
        [Key]
        public string Id { get; set; } = default!;
        public Product Product { get; set; } = default!;
        public string Body { get; set; } = default!;

        [Provides("username")]
        public User Author { get; set; } = default!;
        public string AuthorId { get; set; } = default!;
    }
}
