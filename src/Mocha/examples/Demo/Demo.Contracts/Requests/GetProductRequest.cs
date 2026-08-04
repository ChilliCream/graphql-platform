using Mocha;

namespace Demo.Contracts.Requests;

/// <summary>
/// Request to get product details from the Catalog service.
/// </summary>
public sealed class GetProductRequest : IEventRequest<GetProductResponse>
{
    public required Guid ProductId { get; init; }
}
