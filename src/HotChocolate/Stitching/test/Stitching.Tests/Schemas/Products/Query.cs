using System.Collections.Generic;

namespace HotChocolate.Stitching.Schemas.Products
{
    public class Query
    {
        public IEnumerable<Product> GetTopProducts(
            int first, 
            [Service] ProductRepository repository) =>
            repository.GetTopProducts(first);

        public Product GetProduct(
            int upc, 
            [Service] ProductRepository repository) =>
            repository.GetProduct(upc);
    }
}