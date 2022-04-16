using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Coordinates;

internal class SchemaCoordinatePrinter
{
    public static string Print(ISchemaCoordinate2? coordinate)
    {
        var coordinates = new Stack<ISchemaCoordinate2>();

        while (true)
        {
            if (coordinate is null)
            {
                break;
            }

            coordinates.Push(coordinate);
            coordinate = coordinate.Parent;
        }

        return Print(coordinates);
    }

    public static string Print(IEnumerable<ISchemaCoordinate2> coordinates)
    {
        IEnumerable<ISchemaCoordinate2> filteredCoordinates = coordinates
            .Where(x => x.Kind != SyntaxKind.Document);

        return string.Join(".", filteredCoordinates);
    }
}
