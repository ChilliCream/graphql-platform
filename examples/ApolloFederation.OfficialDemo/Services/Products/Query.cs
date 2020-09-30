using System.Collections.Generic;
using HotChocolate;
using Products.Data;
using Products.Models;

namespace Products
{
    public class Query
    {
        public IEnumerable<Product> GetTopProducts(
            [Service] ProductRepository productRepository,
            int? first)
        {
            return productRepository.GetTop(first ?? 5);
        }
    }
}
