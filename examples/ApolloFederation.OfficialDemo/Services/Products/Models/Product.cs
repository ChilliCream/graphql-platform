using System;
using System.Linq;
using HotChocolate;
using HotChocolate.ApolloFederation;
using HotChocolate.Language;
using Products.Data;

namespace Products.Models
{
    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public class Product
    {
        [Key]
        public string Upc { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public int Weight { get; set; }

        public static Product GetAsync(
            [LocalState] ObjectValueNode data,
            [Service] ProductRepository productRepository)
        {
            if (data.Fields
                    .SingleOrDefault(field => field.Name.Value == "upc") is var field &&
                field is not null &&
                field.Value.Value is string upc)
            {
                return productRepository.GetById(upc);
            }
            throw new ArgumentException();
        }
    }
}
