namespace HotChocolate.Data
{
    public class Book
    {
        public int Id { get; set; }

        public int AuthorId { get; set; }

        public string? Title { get; set; }

        public virtual Author? Author { get; set; }
    }
}
