using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Products;

[GraphQLName("Query")]
public sealed class ProductQuery
{
    public IEnumerable<Product> GetTopProducts(
        int first,
        [Service] ProductRepository repository)
        => repository.GetTopProducts(first);

    [NodeResolver]
    public Product? GetProductById(
        int id,
        [Service] ProductRepository repository)
        => repository.GetProductById(id);
}

[GraphQLName("Mutation")]
public sealed class ProductMutation
{
    public MutationResult<bool, ProductNotFoundError> UploadProductPicture(int productId, IFile file)
    {
        if (productId is 0)
        {
            return new ProductNotFoundError(0, "broken");
        }
        
        return true;
    }
}

public sealed record ProductNotFoundError(int ProductId, string Message);