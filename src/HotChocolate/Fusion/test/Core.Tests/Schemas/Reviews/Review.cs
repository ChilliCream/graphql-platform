namespace HotChocolate.Fusion.Schemas.Reviews;

public record Review(int Id, Author Author, Product Product, string Body);
