namespace HotChocolate.Fusion.Shared.Authors;

public class Author
{
    public string Id { get; set;}

    public string Name { get; set; }

    public string Bio { get; set;}

    public Author(string id, string name, string bio) {
        this.Id = id;
        this.Name = name;
        this.Bio = bio;
    }
}
