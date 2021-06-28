using NetTopologySuite.Geometries;

namespace Spatial.Demo {
    public class GolfCourse {
        public int Xid { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public int Par { get; set; }
        public int Holes { get; set; }
        public MultiPolygon Shape { get; set; }
    }
}
