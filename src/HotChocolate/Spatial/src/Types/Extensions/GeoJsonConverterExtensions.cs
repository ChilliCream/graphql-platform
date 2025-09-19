using System.Collections;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Serialization;

internal static class GeoJsonConverterExtensions
{
    public static bool TryConvertToCoordinates(
        this IList coordinatesList,
        out Coordinate[] coordinates)
    {
        if (coordinatesList.Count == 0)
        {
            coordinates = [];
            return true;
        }

        coordinates = new Coordinate[coordinatesList.Count];
        for (var i = 0; i < coordinates.Length; i++)
        {
            if (coordinatesList[i] is Coordinate c)
            {
                coordinates[i] = c;
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}
