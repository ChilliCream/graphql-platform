namespace StrawberryShake.CodeGeneration.CSharp;

public static class Keywords
{
    private static readonly HashSet<string> _keywords =
    [
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "record",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while",
        "add",
        "alias",
        "ascending",
        "async",
        "await",
        "by",
        "descending",
        "dynamic",
        "equals",
        "from",
        "get",
        "global",
        "group",
        "init",
        "into",
        "join",
        "let",
        "nameof",
        "nint",
        "notnull",
        "nuint",
        "on",
        "orderby",
        "partial",
        "remove",
        "select",
        "set",
        "unmanaged",
        "value",
        "var",
        "when",
        "where",
        "with",
        "yield",
    ];

    public static string ToSafeName(string name)
    {
        if (_keywords.Contains(name))
        {
            return $"@{name}";
        }

        return name;
    }

    public static string ToEscapedName(this string name)
    {
        if (_keywords.Contains(name))
        {
            return $"@{name}";
        }

        return name;
    }
}
