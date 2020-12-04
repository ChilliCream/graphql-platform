using NetTopologySuite.Geometries;

namespace Spatial.Demo
{
    public class Parcel
    {
        public int Xid { get; set; }
        public string ParcelId { get; set; }
        public int BuildingSqFt { get; set; }
        public int Floors { get; set; }
        public string RoomCount { get; set; }
        public decimal? MarketValue { get; set; }
        public int YearBuilt { get; set; }
        public MultiPolygon Shape { get; set; }
    }
}
