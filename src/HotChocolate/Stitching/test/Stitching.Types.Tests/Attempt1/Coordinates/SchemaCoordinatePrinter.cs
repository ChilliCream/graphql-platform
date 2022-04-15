using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal class SchemaCoordinatePrinter
{
    public static readonly NameNode Root = new("Root");

    public static string Print(ISchemaCoordinate2? coordinate)
    {
        var coordinates = new Stack<ISchemaCoordinate2>();

        while (true)
        {
            if (coordinate is null)
            {
                break;
            }

            if (Root.Equals(coordinate.Name))
            {
                break;
            }

            coordinates.Push(coordinate);
            coordinate = coordinate.Parent;
        }

        return string.Join(".", coordinates);
    }
}
