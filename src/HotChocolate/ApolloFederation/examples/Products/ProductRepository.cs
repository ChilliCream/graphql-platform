namespace Products;

public class ProductRepository
{
    private Dictionary<string, Product> _products;

    public ProductRepository()
    {
        _products = CreateProducts().ToDictionary(product => product.Upc);
    }

    public IEnumerable<Product> GetTop(int amount)
        => _products.Values.Take(amount);

    public Product GetById(string upc)
        => _products[upc];

    private static IEnumerable<Product> CreateProducts()
    {
        yield return new Product
        {
            Upc = "1",
            Name = "Table",
            Price = 899,
            Weight = 100,
        };

        yield return new Product
        {
            Upc = "2",
            Name = "Couch",
            Price = 1299,
            Weight = 1000,
        };

        yield return new Product
        {
            Upc = "3",
            Name = "Chair",
            Price = 54,
            Weight = 50,
        };
    }
}
