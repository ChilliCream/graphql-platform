using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Stitching.Schemas.Inventory
{
    public class InventoryInfoRepository
    {
        private readonly Dictionary<int, InventoryInfo> _infos;

        public InventoryInfoRepository()
        {
            _infos = new InventoryInfo[]
            {
                new InventoryInfo(1, true),
                new InventoryInfo(2, false),
                new InventoryInfo(3, true)
            }.ToDictionary(t => t.Upc);
        }

        public InventoryInfo GetInventoryInfo(int upc) => _infos[upc];
    }
}