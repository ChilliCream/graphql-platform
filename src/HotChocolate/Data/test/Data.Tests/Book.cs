namespace HotChocolate.Data
{
    public class Book
    {
        public int Id { get; set; }

        public string? Title { get; set; }

        public virtual Author? Author { get; set; }

        public virtual Publisher? Publisher { get; set; }
    }
}
