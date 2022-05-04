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

    public string Id { get; }

    public string Body { get; }

    public string AuthorId { get; }

    public Product Product { get; }
}
