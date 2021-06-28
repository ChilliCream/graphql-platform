using NetTopologySuite.Geometries;

namespace Spatial.Demo {
    public class LiquorStore {
        public int Xid { get; set; }
        public int StoreNumber { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string Phone { get; set; }
        public int Zip { get; set; }
        public Point Shape { get; set; }
    }
}
