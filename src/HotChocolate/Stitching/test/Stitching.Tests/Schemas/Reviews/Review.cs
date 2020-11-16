namespace HotChocolate.Stitching.Schemas.Reviews
{
    public class Review
    {
        public Review(int id, int authorId, int upc, string body)
        {
            Id = id;
            AuthorId = authorId;
            Upc = upc;
            Body = body;
        }

        public int Id { get; }
        public int AuthorId { get; } 
        public int Upc { get; } 
        public string Body { get; }
    }
}