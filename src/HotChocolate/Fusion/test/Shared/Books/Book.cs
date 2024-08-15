namespace HotChocolate.Fusion.Shared.Books;

public class Book
{
    public string Id { get; set;}

    public string AuthorId { get; set;}

    public string Title {get; set; }

    public Book(string id, string authorId, string title) {
        this.Id = id;
        this.AuthorId = authorId;
        this.Title = title;
    }
}
