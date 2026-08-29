using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Helpers;

internal static class CSharpLiteralFormatter
{
    public static string FormatTypedConstant(TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return "null";
        }

        return constant.Kind switch
        {
            TypedConstantKind.Primitive => FormatPrimitive(constant.Value),
            TypedConstantKind.Enum => FormatEnumConstant(constant),
            TypedConstantKind.Type => $"typeof({((ITypeSymbol)constant.Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})",
            TypedConstantKind.Array => FormatArray(constant),
            _ => constant.Value?.ToString() ?? "null"
        };
    }

    public static string FormatPrimitive(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        return value switch
        {
            string s => $"\"{EscapeString(s)}\"",
            char c => $"'{EscapeChar(c)}'",
            bool b => b ? "true" : "false",
            float f => $"{f}f",
            double d => $"{d}d",
            decimal m => $"{m}m",
            long l => $"{l}L",
            ulong ul => $"{ul}UL",
            _ => value.ToString() ?? "null"
        };
    }

    private static string FormatArray(TypedConstant constant)
    {
        var elements = constant.Values;
        if (elements.IsDefaultOrEmpty)
        {
            var elementType = ((IArrayTypeSymbol?)constant.Type)?.ElementType
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return $"new {elementType}[] {{ }}";
        }

        var elementStrings = string.Join(", ", elements.Select(FormatTypedConstant));
        return $"new[] {{ {elementStrings} }}";
    }

    private static string FormatEnumConstant(TypedConstant constant)
    {
        if (constant.Type is not INamedTypeSymbol enumSymbol)
        {
            return FormatPrimitive(constant.Value);
        }

        var enumType = enumSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (constant.Value is not null)
        {
            foreach (var member in enumSymbol.GetMembers())
            {
                if (member is IFieldSymbol { HasConstantValue: true } field
                    && Equals(field.ConstantValue, constant.Value))
                {
                    return $"{enumType}.{field.Name}";
                }
            }
        }

        return $"({enumType}){constant.Value}";
    }

    private static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string EscapeChar(char c)
    {
        return c switch
        {
            '\\' => "\\\\",
            '\'' => "\\'",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ => c.ToString()
        };
    }
}
