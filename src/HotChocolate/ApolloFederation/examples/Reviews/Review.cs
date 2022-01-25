using HotChocolate.ApolloFederation;

namespace Reviews;

public class Review
{
    public Review(string id, string body, string authorId, string upc)
    {
        Id = id;
        Body = body;
        AuthorId = authorId;
        Product = new Product(upc);
    }

    [Key]
    public string Id { get; }

    public string Body { get; }
    
    public string AuthorId { get; }

    public Product Product { get; }

    [Provides("username")]
    public Task<User> GetAuthorAsync(
        UserRepository repository) 
        => repository.GetUserById(AuthorId);
}
