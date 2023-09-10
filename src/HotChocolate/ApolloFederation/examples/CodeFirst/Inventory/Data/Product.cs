namespace Inventory;

public class Product
{
    public Product(string upc)
    {
        Upc = upc;
    }

    public string Upc { get; }

    public int Weight { get; private set; }

    public int Price { get; private set; }

    public bool InStock => true;
}
