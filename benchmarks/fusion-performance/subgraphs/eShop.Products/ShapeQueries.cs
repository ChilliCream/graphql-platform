using System.Collections.Concurrent;
using HotChocolate.Types;

namespace eShop.Products;

[QueryType]
public static partial class ShapeQueries
{
    private static readonly ConcurrentDictionary<int, string> s_strings = new();
    private static readonly ConcurrentDictionary<int, Product[]> s_lists = new();

    public static string ShapeEcho(string text, int number, bool flag, double ratio, ShapeInput input)
        => $"{text}:{number}:{flag}:{ratio}:{input.Text}:{input.Nested.Count}";

    public static string ShapeScalar(int bytes)
        => s_strings.GetOrAdd(bytes, static size => new string('x', size));

    public static Product[] ShapeProducts(int count)
        => s_lists.GetOrAdd(count, static size => Enumerable.Range(1, size)
            .Select(i => new Product { Upc = i.ToString(), Name = "Product", Price = i, Weight = i })
            .ToArray());
}

public sealed record ShapeInput(string Text, ShapeNestedInput Nested);
public sealed record ShapeNestedInput(int Count, string[] Tags);
