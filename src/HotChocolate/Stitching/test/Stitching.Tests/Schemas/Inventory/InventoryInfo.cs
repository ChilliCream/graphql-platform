namespace HotChocolate.Stitching.Schemas.Inventory
{
    public class InventoryInfo
    {
        public InventoryInfo(int upc, bool isInStock)
        {
            Upc = upc;
            IsInStock = isInStock;
        }

        public int Upc { get; }

        public bool IsInStock { get; }
    }
}
