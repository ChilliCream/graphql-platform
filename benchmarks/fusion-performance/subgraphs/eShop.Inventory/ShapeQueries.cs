using HotChocolate.Types;

namespace eShop.Inventory;

[QueryType]
public static partial class ShapeQueries
{
    public static string InventoryShapeEcho(string text, int number, bool flag, double ratio, ShapeInput input)
        => $"{text}:{number}:{flag}:{ratio}:{input.Text}:{input.Nested.Count}";
}

public sealed record ShapeInput(string Text, ShapeNestedInput Nested);
public sealed record ShapeNestedInput(int Count, string[] Tags);
