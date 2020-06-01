namespace HotChocolate.Spatial.Types.Filters
{
    public class FilterDistance
    {
        public FilterDistance(
            FilterPointData from)
        {
            From = from;
        }

        public FilterPointData From { get; }

        public double Is { get; set; }
    }
}