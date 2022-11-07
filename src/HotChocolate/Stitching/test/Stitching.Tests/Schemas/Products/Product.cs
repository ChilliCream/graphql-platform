namespace HotChocolate.Stitching.Schemas.Products
{
    public class Product
    {
        public Product(int upc, string name, int price, int weight)
        {
            Upc = upc;
            Name = name;
            Price = price;
            Weight = weight;
        }

        public int Upc { get; }
        public string Name { get; }
        public int Price { get; }
        public int Weight { get; }
    }
}
