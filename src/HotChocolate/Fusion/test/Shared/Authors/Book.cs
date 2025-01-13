namespace HotChocolate.Fusion.Shared.Authors;

public class Book
{
    public string AuthorId { get; set;}

    public Author Author {get; set; }

    public Book(string authorId, Author author) {
        this.AuthorId = authorId;
        this.Author = author;
    }
}
