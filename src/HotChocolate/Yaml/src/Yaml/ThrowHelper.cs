namespace HotChocolate.Text.Yaml;

internal static class ThrowHelper
{
    public static YamlException TabIndentation(int line, int column)
        => new("Tabs cannot be used for indentation", line, column);

    public static YamlException DocumentMarkersNotSupported(int line, int column)
        => new("Document markers are not supported", line, column);

    public static YamlException UnexpectedIndentation(int line, int column)
        => new("The line has an unexpected indentation", line, column);

    public static YamlException ExpectedKey(int line, int column)
        => new("Expected a key followed by ':'", line, column);

    public static YamlException DuplicateKey(string key, int line, int column)
        => new($"The key '{key}' is defined more than once", line, column);

    public static YamlException UnterminatedString(int line, int column)
        => new("The double-quoted string does not end on the same line", line, column);

    public static YamlException InvalidEscapeSequence(int line, int column)
        => new("The escape sequence is not valid", line, column);

    public static YamlException ColonInPlainScalar(int line, int column)
        => new("A value without quotes cannot contain ': ' or end with ':'", line, column);

    public static YamlException UnexpectedCharacter(char character, int line, int column)
        => new($"Unexpected character '{character}'", line, column);

    public static YamlException AnchorsNotSupported(int line, int column)
        => new("Anchors are not supported", line, column);

    public static YamlException AliasesNotSupported(int line, int column)
        => new("Aliases are not supported", line, column);

    public static YamlException TagsNotSupported(int line, int column)
        => new("Tags are not supported", line, column);

    public static YamlException BlockScalarsNotSupported(int line, int column)
        => new("Block scalars are not supported", line, column);

    public static YamlException SingleQuotedScalarsNotSupported(int line, int column)
        => new("Single-quoted strings are not supported. Use double quotes", line, column);

    public static YamlException FlowCollectionsNotSupported(int line, int column)
        => new(
            "Flow collections are not supported, except [] and {} for empty collections",
            line,
            column);

    public static InvalidOperationException WrongKind(YamlValueKind expected, YamlValueKind actual)
        => new($"The operation requires an element of kind '{expected}', but the element is of kind '{actual}'.");

    public static InvalidOperationException UndefinedElement()
        => new("The element has no value.");
}
