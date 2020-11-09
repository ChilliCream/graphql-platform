using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Stitching.Schemas.Products
{
    public class ProductRepository
    {
        private readonly Dictionary<int, Product> _products;

        public ProductRepository()
        {
            _products = new Product[]
            {
                new Product(1, "Table", 899, 100),
                new Product(2, "Couch", 1299, 1000),
                new Product(3, "Chair", 54, 50)
            }.ToDictionary(t => t.Upc);
        }

        [GraphQLNonNullType]
        public IEnumerable<Product> GetTopProducts(int first) =>
            _products.Values.OrderBy(t => t.Upc).Take(first);

        public Product GetProduct   (int upc) => _products[upc];
    }
}
