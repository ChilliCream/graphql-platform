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

    [NodeResolver]
    public ProductConfiguration? GetProductConfigurationByUsername(
        string username,
        [Service] ProductRepository repository)
        => repository.GetProductConfigurationByUsername(username);

    [NodeResolver]
    public ProductBookmark? GetProductBookmarkByUsername(
        string username,
        [Service] ProductRepository repository)
        => repository.GetProductBookmarkByUsername(username);
}

[GraphQLName("Mutation")]
public sealed class ProductMutation
{
    public FieldResult<bool, ProductNotFoundError> UploadProductPicture(int productId, IFile file)
    {
        if (productId is 0)
        {
            return new ProductNotFoundError(0, "broken");
        }

        return true;
    }

    public FieldResult<bool, ProductNotFoundError> UploadMultipleProductPictures(IList<ProductIdWithUpload> products)
    {
        if (products.Any(x => x.productId is 0))
        {
            return new ProductNotFoundError(0, "broken");
        }

        return true;
    }
}

public sealed record ProductNotFoundError(int ProductId, string Message);

public sealed record ProductIdWithUpload(int productId, IFile file);
