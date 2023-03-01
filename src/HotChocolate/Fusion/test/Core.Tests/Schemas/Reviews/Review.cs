namespace HotChocolate.Fusion.Schemas.Reviews;

public record Review(int Id, int AuthorId, int Upc, string Body);
