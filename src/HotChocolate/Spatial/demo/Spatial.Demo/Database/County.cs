using NetTopologySuite.Geometries;

namespace Spatial.Demo
{
    public class County
    {
        public int Xid { get; set; }

        public string Countynbr { get; set; }

        public decimal? Entitynbr { get; set; }

        public decimal? Entityyr { get; set; }

        public string Name { get; set; }

        public decimal? Fips { get; set; }

        public string Stateplane { get; set; }

        public decimal? PopLastcensus { get; set; }

        public decimal? PopCurrestimate { get; set; }

        public string FipsStr { get; set; }

        public decimal? Color4 { get; set; }

        public MultiPolygon Shape { get; set; }
    }
}
