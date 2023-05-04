using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Reviews;

[Node]
public record Review(int Id, Author Author, Product Product, string Body) : IReviewOrAuthor;
