
using System;
using System.Linq;
using HotChocolate.Types;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Filtering.Customization
{
    [ExtendObjectType(Name = "Query")]
    public class TouristAttractionQueries
    {
        private static readonly GeometryFactory _geometryFactory =
            NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        private static readonly TouristAttraction[] _attractions = new TouristAttraction[]
        {
            new TouristAttraction(
                1,
                "Taj Mahal",
                _geometryFactory.CreatePoint(new Coordinate(27.175015, 78.042155))
            ),
            new TouristAttraction(
                2,
                "The Golden Temple of Amritsar",
                _geometryFactory.CreatePoint(new Coordinate(31.619980, 74.876485))),
            new TouristAttraction(
                3,
                "The Red Fort, New Delhi",
                _geometryFactory.CreatePoint(new Coordinate(28.656159, 77.241020))
            ),
            new TouristAttraction(
                4,
                "The Gateway of India Mumbai",
                _geometryFactory.CreatePoint(new Coordinate(18.921984, 72.834654))
            ),
            new TouristAttraction(
                5,
                "Mysore Palace",
                _geometryFactory.CreatePoint(new Coordinate(12.305025, 76.655753))
            ),
            new TouristAttraction(
                6,
                "Qutb Minar",
                _geometryFactory.CreatePoint(new Coordinate(28.524475, 77.185521))
            )
        };

        [UseFiltering(FilterType = typeof(TourstAttractionFilterType))]
        public IQueryable<TouristAttraction> GetTouristAttractions() => _attractions.AsQueryable();
    }
}