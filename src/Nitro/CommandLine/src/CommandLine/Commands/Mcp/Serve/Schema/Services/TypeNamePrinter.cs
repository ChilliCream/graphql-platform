using HotChocolate.Language;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

internal static class TypeNamePrinter
{
    public static string Print(ITypeNode type)
        => type switch
        {
            NonNullTypeNode nonNull => Print(nonNull.Type) + "!",
            ListTypeNode list => "[" + Print(list.Type) + "]",
            NamedTypeNode named => named.Name.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
}
