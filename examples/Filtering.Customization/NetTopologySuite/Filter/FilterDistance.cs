namespace Filtering.Customization
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