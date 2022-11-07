namespace HotChocolate.Stitching.Schemas.Reviews
{
    public class Author
    {
        public Author(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }
    }
}
