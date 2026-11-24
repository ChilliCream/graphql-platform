namespace HotChocolate.Text.Yaml;

internal static class ThrowHelper
{
    public static YamlException TabIndentation(int line, int column)
        => Create("Tabs cannot be used for indentation", line, column);

    public static YamlException DocumentMarkersNotSupported(int line, int column)
        => Create("Document markers are not supported", line, column);

    public static YamlException UnexpectedIndentation(int line, int column)
        => Create("The line has an unexpected indentation", line, column);

    public static YamlException ExpectedKey(int line, int column)
        => Create("Expected a key followed by ':'", line, column);

    public static YamlException DuplicateKey(string key, int line, int column)
        => Create($"The key '{key}' is defined more than once", line, column);

    public static YamlException UnterminatedString(int line, int column)
        => Create("The double-quoted string does not end on the same line", line, column);

    public static YamlException InvalidEscapeSequence(int line, int column)
        => Create("The escape sequence is not valid", line, column);

    public static YamlException ColonInPlainScalar(int line, int column)
        => Create("A value without quotes cannot contain ': ' or end with ':'", line, column);

    public static YamlException UnexpectedCharacter(char character, int line, int column)
        => Create($"Unexpected character '{character}'", line, column);

    public static YamlException AnchorsNotSupported(int line, int column)
        => Create("Anchors are not supported", line, column);

    public static YamlException AliasesNotSupported(int line, int column)
        => Create("Aliases are not supported", line, column);

    public static YamlException TagsNotSupported(int line, int column)
        => Create("Tags are not supported", line, column);

    public static YamlException BlockScalarsNotSupported(int line, int column)
        => Create("Block scalars are not supported", line, column);

    public static YamlException SingleQuotedScalarsNotSupported(int line, int column)
        => Create("Single-quoted strings are not supported. Use double quotes", line, column);

    public static YamlException FlowCollectionsNotSupported(int line, int column)
        => Create(
            "Flow collections are not supported, except [] and {} for empty collections",
            line,
            column);

    public static InvalidOperationException WrongKind(YamlValueKind expected, YamlValueKind actual)
        => new($"The operation requires an element of kind '{expected}', but the element is of kind '{actual}'.");

    public static InvalidOperationException UndefinedElement()
        => new("The element has no value.");

    private static YamlException Create(string message, int line, int column)
        => new($"{message} (line {line}, column {column}).", line, column);
}
