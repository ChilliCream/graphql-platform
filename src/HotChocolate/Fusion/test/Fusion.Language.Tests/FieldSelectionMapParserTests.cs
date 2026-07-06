namespace HotChocolate.Fusion.Language;

public sealed class FieldSelectionMapParserTests
{
    [Fact]
    public void Parse_PathSegmentSingleFieldName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_PathSegmentNestedFieldName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_PathSegmentWithTypeName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_PathSegmentWithTwoTypeNames_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2<Type2>.field3");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_PathSegmentWithTypeNameNoNestedField_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act() => new FieldSelectionMapParser("field1<Type1>").Parse();

        // assert
        Assert.Equal(
            "Expected a `Period`-token, but found a `EndOfFile`-token.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Parse_PathWithTypeName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("<Type1>.field1");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Theory]
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-Path
    [InlineData("book.title")]
    [InlineData("mediaById<Book>.isbn")]
    public void ParseAndPrint_PathValidExamples_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(sourceText, result);
    }

    [Fact]
    public void Parse_PathWithTypeNameNoPathSegment_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act() => new FieldSelectionMapParser("<Type1>").Parse();

        // assert
        Assert.Equal(
            "Expected a `Period`-token, but found a `EndOfFile`-token.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Parse_SelectedListValue_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1[field2]");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Theory]
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-SelectedListValue
    [InlineData("parts[id]")]
    [InlineData("parts[{ id, name }]")]
    [InlineData("parts[[{ id, name }]]")]
    [InlineData("{ coordinates: coordinates[{ lat: x, lon: y }] }")]
    public void ParseAndPrint_SelectedListValueValidExamples_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(sourceText, result);
    }

    [Theory]
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-SelectedListValue
    [InlineData("parts[id, name]")]
    public void Parse_SelectedListValueInvalidExamples_ThrowsSyntaxException(string sourceText)
    {
        // arrange & act
        void Act() => new FieldSelectionMapParser(sourceText).Parse();

        // assert
        Assert.Equal(
            "Expected a `RightSquareBracket`-token, but found a `Name`-token.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Parse_SelectedObjectValue_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("{ field1: field1 }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_SelectedObjectValueNoSelectedValue_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("{ field1 }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_SelectedObjectValueMultipleFieldsNoSelectedValue_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("{ field1, field2 }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Theory]
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-SelectedObjectValue
    [InlineData("dimension.{ size, weight }")]
    [InlineData("{ size: dimensions.size, weight: dimensions.weight }")]
    // a path segment carrying arguments immediately before a "." object selection
    [InlineData("a(x: 1).{ b }")]
    public void ParseAndPrint_SelectedObjectValueValidExamples_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(sourceText, result);
    }

    [Fact]
    public void Parse_SelectedValueMultiplePaths_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2 | field1<Type2>.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_SelectedValueMultipleSelectedObjectValues_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("{ field1: field1 } | { field2: field2 }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_SelectedValueMultipleSelectedObjectValuesNested_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser(
            "{ nested: { field1: field1 } | { field2: field2 } }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Theory]
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-SelectedValue
    [InlineData("mediaById<Book>.title | mediaById<Movie>.movieTitle")]
    [InlineData("{ movieId: <Movie>.id } | { productId: <Product>.id }")]
    [InlineData("{ nested: { movieId: <Movie>.id } | { productId: <Product>.id } }")]
    public void ParseAndPrint_SelectedValueValidExamples_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(sourceText, result);
    }

    [Fact]
    public void Parse_FieldWithArgument_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("width(unit: IMPERIAL)");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Theory]
    // int
    [InlineData("f(a: 1)")]
    [InlineData("f(a: 0)")]
    [InlineData("f(a: -1)")]
    [InlineData("f(a: -0)")]
    // float
    [InlineData("f(a: 1.5)")]
    [InlineData("f(a: -1.5)")]
    [InlineData("f(a: 1.0)")]
    [InlineData("f(a: 1e3)")]
    [InlineData("f(a: 1.5e3)")]
    [InlineData("f(a: 1.5e-3)")]
    [InlineData("f(a: -1.5E+3)")]
    // string (escaping is idempotent, so these round-trip byte-for-byte)
    [InlineData("f(a: \"x\")")]
    [InlineData("f(a: \"\")")]
    [InlineData("f(a: \"with \\\"quote\\\"\")")]
    [InlineData("f(a: \"tab\\tnl\\n\")")]
    [InlineData("f(a: \"A\")")]
    [InlineData("f(a: \"a\\\\b\")")]
    // boolean / null
    [InlineData("f(a: true)")]
    [InlineData("f(a: false)")]
    [InlineData("f(a: null)")]
    // enum
    [InlineData("f(a: ENUM_VALUE)")]
    // list
    [InlineData("f(a: [])")]
    [InlineData("f(a: [1])")]
    [InlineData("f(a: [1, 2, 3])")]
    [InlineData("f(a: [[1], [2, 3]])")]
    [InlineData("f(a: [1, \"x\", true, null, E])")]
    [InlineData("f(a: [{ a: 1 }, { a: 2 }])")]
    // object
    [InlineData("f(a: {})")]
    [InlineData("f(a: { a: 1 })")]
    [InlineData("f(a: { a: 1, b: \"x\", c: true })")]
    [InlineData("f(a: { a: { b: { c: 1 } } })")]
    [InlineData("f(a: { a: [1, 2], b: { c: E } })")]
    public void ParseAndPrint_ArgumentValueKinds_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(sourceText, result);
    }

    [Theory]
    // multiple args
    [InlineData("f(a: 1, b: 2, c: \"x\")")]
    // path terminal segment
    [InlineData("width(unit: IMPERIAL)")]
    // path intermediate segment
    [InlineData("packaging(material: BOX).weight")]
    // multiple segments with args
    [InlineData("a(x: 1).b(y: 2).c")]
    // args + type narrowing
    [InlineData("field(x: 1)<SomeType>.other")]
    [InlineData("<SomeType>.field(x: 1)")]
    // shorthand object field
    [InlineData("{ width(unit: IMPERIAL) }")]
    // shorthand in list selection
    [InlineData("dimensions[{ width(unit: IMPERIAL), height(unit: IMPERIAL) }]")]
    // labeled + shorthand mix
    [InlineData("{ a: b(x: 1), c(y: 2) }")]
    // all kinds in one arg list
    [InlineData(
        "f(i: 1, fl: 1.5, s: \"x\", b: true, n: null, e: E, l: [1, 2], "
        + "o: { k: 1 })")]
    // duplicate names are accepted by the parser; name uniqueness is enforced by validation, not syntax.
    [InlineData("f(a: 1, a: 2)")]
    [InlineData("f(o: { k: 1, k: 2 })")]
    public void ParseAndPrint_ArgumentPlacementAndCount_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(sourceText, result);
    }

    [Theory]
    // full @require example
    [InlineData("{ width: width(unit: IMPERIAL), height: height(unit: IMPERIAL) }")]
    // path + list selection with args at multiple levels (shorthand object field)
    [InlineData("a(x: 1).b[{ c(y: 2) }]")]
    // choice with args in branches
    [InlineData("a(x: 1) | b(y: 2)")]
    // deeply nested object selection carrying argument-bearing shorthand fields
    [InlineData("{ outer: { inner: { width(unit: IMPERIAL) } } }")]
    public void ParseAndPrint_ArgumentsWithSelectionMapConstructs_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(sourceText, result);
    }

    [Theory]
    // Arguments are only valid on the shorthand object field form ("Name Arguments?"),
    // not on a field that also selects an explicit value ("Name : SelectedValue").
    [InlineData("{ a(x: 1): b }")]
    [InlineData("a(x: 1).b[{ c(y: 2): d }]")]
    public void Parse_ArgumentsOnObjectFieldWithValue_ThrowsSyntaxException(string sourceText)
    {
        // arrange & act
        void Act() => new FieldSelectionMapParser(sourceText).Parse();

        // assert
        Assert.Equal(
            "Arguments are not allowed on an object field that selects an explicit value.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Theory]
    // Arguments must come before the type condition, so a `(` after `<Type>` is rejected.
    [InlineData("field1<Type1>(x: 1).field2")]
    [InlineData("<Type1>(x: 1).field1")]
    public void Parse_ArgumentsAfterTypeCondition_ThrowsSyntaxException(string sourceText)
    {
        // arrange & act
        void Act() => new FieldSelectionMapParser(sourceText).Parse();

        // assert
        Assert.Equal(
            "Expected a `Period`-token, but found a `LeftParenthesis`-token.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Theory]
    // empty Arguments must NOT render "()"
    [InlineData("field")]
    [InlineData("{ a }")]
    [InlineData("{ a: b }")]
    public void ParseAndPrint_NoArguments_DoesNotRenderParentheses(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(sourceText, result);
    }

    [Fact]
    public void Parse_BooleanAndNullKeywords_ParseToBooleanAndNullNodes()
    {
        // arrange
        var trueNode = (PathNode)new FieldSelectionMapParser("f(a: true)").Parse();
        var falseNode = (PathNode)new FieldSelectionMapParser("f(a: false)").Parse();
        var nullNode = (PathNode)new FieldSelectionMapParser("f(a: null)").Parse();
        var enumNode = (PathNode)new FieldSelectionMapParser("f(a: TRUEISH)").Parse();

        // act
        var trueValue = trueNode.PathSegment.Arguments[0].Value;
        var falseValue = falseNode.PathSegment.Arguments[0].Value;
        var nullValue = nullNode.PathSegment.Arguments[0].Value;
        var enumValue = enumNode.PathSegment.Arguments[0].Value;

        // assert
        Assert.Multiple(
            () => Assert.Equal(FieldSelectionMapSyntaxKind.BooleanValue, trueValue.Kind),
            () => Assert.Equal(FieldSelectionMapSyntaxKind.BooleanValue, falseValue.Kind),
            () => Assert.Equal(FieldSelectionMapSyntaxKind.NullValue, nullValue.Kind),
            () => Assert.Equal(FieldSelectionMapSyntaxKind.EnumValue, enumValue.Kind));
    }

    [Theory]
    // An escape-bearing string is canonicalized to its escaped double-quoted form on print.
    // \uXXXX escape decodes then re-emits the literal character (printable)
    [InlineData("f(a: \"\\u0041\")", "f(a: \"A\")")]
    [InlineData("f(a: \"\\u20AC\")", "f(a: \"€\")")]
    // Commas between arguments are insignificant whitespace, so the printed form normalizes
    // both the missing-comma and the doubled-comma input to a single comma separator.
    [InlineData("f(a: 1 b: 2)", "f(a: 1, b: 2)")]
    [InlineData("f(a: 1,, b: 2)", "f(a: 1, b: 2)")]
    public void ParseAndPrint_CanonicalizesValue_Matches(string sourceText, string expected)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(expected, result);
    }

    [Theory]
    // Block strings are printed in HotChocolate's multi-line triple-quote layout (real newlines),
    // even in compact mode. Newlines are normalized to \n so the assertion is platform-agnostic.
    [InlineData("f(a: \"\"\"x\"\"\")", "f(a: \"\"\"\nx\n\"\"\")")]
    [InlineData(
        "f(a: \"\"\"has \"quote\" inside\"\"\")",
        "f(a: \"\"\"\nhas \"quote\" inside\n\"\"\")")]
    [InlineData("f(a: \"\"\"multi\nline\"\"\")", "f(a: \"\"\"\nmulti\nline\n\"\"\")")]
    public void ParseAndPrint_BlockString_PrintsHotChocolateLayout(
        string sourceText,
        string expected)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText)
            .Parse()
            .Print(indented: false)
            .Replace("\r\n", "\n");

        // assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseAndPrint_BlockStringWithCommonIndent_DedentsThenPrintsLayout()
    {
        // arrange
        // a block string whose lines after the first share a common indentation.
        const string sourceText =
            "f(a: \"\"\"\n    first line\n    second line\n\"\"\")";

        // act
        var result = new FieldSelectionMapParser(sourceText)
            .Parse()
            .Print(indented: false)
            .Replace("\r\n", "\n");

        // assert
        // the common indentation and blank edge lines are removed on parse, then re-emitted.
        Assert.Equal("f(a: \"\"\"\nfirst line\nsecond line\n\"\"\")", result);
    }

    [Fact]
    public void ParseAndPrint_BlockStringInList_EmbedsBlockLayout()
    {
        // arrange
        const string sourceText = "f(a: [\"\"\"x\"\"\", \"\"\"y\"\"\"])";

        // act
        var result = new FieldSelectionMapParser(sourceText)
            .Parse()
            .Print(indented: false)
            .Replace("\r\n", "\n");

        // assert
        // the Fusion serializer has a single flat layout for constant lists, so the block
        // strings are embedded within it. They parse back because newlines are insignificant
        // whitespace between tokens.
        Assert.Equal("f(a: [\"\"\"\nx\n\"\"\", \"\"\"\ny\n\"\"\"])", result);
    }

    [Theory]
    [InlineData("f(a: \"\"\"x\"\"\")")]
    [InlineData("f(a: \"\"\"multi\nline\"\"\")")]
    [InlineData("f(a: \"\"\"has \"quote\" inside\"\"\")")]
    [InlineData("f(a: [\"\"\"x\"\"\", \"\"\"y\"\"\"])")]
    [InlineData("f(a: { k: \"\"\"x\"\"\" })")]
    public void ParseAndPrint_BlockString_RoundTripsSemantically(string sourceText)
    {
        // arrange
        var firstValue = ParseArgumentValue(sourceText);

        // act
        var printed = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);
        var secondValue = ParseArgumentValue(printed);

        // assert
        // round-trip is semantic, the decoded value survives parse -> print -> parse.
        var first = Assert.IsType<StringValueNode>(FirstStringValue(firstValue));
        var second = Assert.IsType<StringValueNode>(FirstStringValue(secondValue));
        Assert.Equal(first.Value, second.Value);
    }

    private static IValueNode FirstStringValue(IValueNode value)
    {
        return value switch
        {
            ListValueNode list => list.Items[0],
            ObjectValueNode obj => obj.Fields[0].Value,
            _ => value
        };
    }

    [Theory]
    [InlineData("f(a: \"tab\\tnl\\n\")", "tab\tnl\n")]
    [InlineData("f(a: \"with \\\"quote\\\"\")", "with \"quote\"")]
    [InlineData("f(a: \"a\\\\b\")", "a\\b")]
    [InlineData("f(a: \"a\\/b\")", "a/b")]
    [InlineData("f(a: \"\\b\\f\\n\\r\\t\")", "\b\f\n\r\t")]
    [InlineData("f(a: \"\\u0041\")", "A")]
    [InlineData("f(a: \"\\u20AC\")", "€")]
    // surrogate pair arrives as two consecutive \u escapes and is reassembled into the emoji
    [InlineData("f(a: \"\\uD83D\\uDE00\")", "😀")]
    public void ParseStringValue_DecodesEscapesAndUnicode_ValueMatches(
        string sourceText,
        string expectedValue)
    {
        // arrange & act
        var value = ParseArgumentValue(sourceText);

        // assert
        var stringValue = Assert.IsType<StringValueNode>(value);
        Assert.Equal(expectedValue, stringValue.Value);
    }

    [Theory]
    [InlineData("f(a: \"a\\qb\")")]
    [InlineData("f(a: \"a\\u12\")")]
    [InlineData("f(a: \"a\\uZZZZ\")")]
    public void ParseStringValue_InvalidEscape_ThrowsSyntaxException(string sourceText)
    {
        // arrange & act
        void Act() => new FieldSelectionMapParser(sourceText).Parse();

        // assert
        Assert.Throws<FieldSelectionMapSyntaxException>(Act);
    }

    [Theory]
    // A high surrogate must be immediately followed by a \u low surrogate, and a low surrogate
    // may not appear on its own. An unpaired surrogate is rejected rather than passed through.
    [InlineData("f(a: \"\\uD800\")")]
    [InlineData("f(a: \"\\uDC00\")")]
    [InlineData("f(a: \"\\uD800\\uD800\")")]
    [InlineData("f(a: \"\\uD800x\")")]
    // NumberStyles.HexNumber would tolerate whitespace inside the four-digit slice, so a space in
    // place of a hex digit must be rejected (the trailing char keeps the slice four chars long).
    [InlineData("f(a: \"\\u 041\")")]
    [InlineData("f(a: \"\\u041 \")")]
    public void ParseStringValue_InvalidUnicodeEscape_ThrowsSyntaxException(string sourceText)
    {
        // arrange & act
        void Act() => new FieldSelectionMapParser(sourceText).Parse();

        // assert
        Assert.Equal(
            "Invalid Unicode escape sequence.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void ParseStringValue_ValidSurrogatePair_DecodesToEmoji()
    {
        // arrange
        // a code point outside the BMP arrives as a high/low \u surrogate pair.
        const string sourceText = "f(a: \"\\uD83D\\uDE00\")";

        // act
        var value = ParseArgumentValue(sourceText);

        // assert
        var stringValue = Assert.IsType<StringValueNode>(value);
        Assert.Equal("😀", stringValue.Value);
    }

    [Fact]
    public void ParseBlockString_RemovesCommonIndentAndBlankLines_ValueMatches()
    {
        // arrange
        // first line has no indent, the following lines share four leading spaces.
        const string sourceText =
            "f(a: \"\"\"\n    block string uses\n    common indentation\n\"\"\")";

        // act
        var value = ParseArgumentValue(sourceText);

        // assert
        var stringValue = Assert.IsType<StringValueNode>(value);
        Assert.Equal("block string uses\ncommon indentation", stringValue.Value);
    }

    [Theory]
    // Dedent follows the GraphQL specification: whitespace-only lines are excluded from the
    // common indentation and whitespace-only edge lines are trimmed. HotChocolate currently
    // differs (it counts whitespace-only lines toward the common indent and only trims fully
    // empty edge lines), so these inputs intentionally pin the spec behavior.
    [InlineData("\n    abc\n  \n", "abc")]
    [InlineData("\n   \nabc", "abc")]
    public void ParseBlockString_DedentDivergesFromHotChocolate_ValueMatches(
        string blockContent,
        string expectedValue)
    {
        // arrange
        var sourceText = $"f(a: \"\"\"{blockContent}\"\"\")";

        // act
        var value = ParseArgumentValue(sourceText);

        // assert
        var stringValue = Assert.IsType<StringValueNode>(value);
        Assert.Equal(expectedValue, stringValue.Value);
    }

    [Fact]
    public void ParseAndPrint_SingleLineBlockString_TrimsValueOnPrint()
    {
        // arrange
        // a single-line block string keeps its surrounding spaces in the value but the printer
        // trims them, mirroring HotChocolate's SyntaxWriterExtensions.WriteStringValue.
        const string sourceText = "f(a: \"\"\"  x  \"\"\")";

        // act
        var value = ParseArgumentValue(sourceText);
        var printed = new FieldSelectionMapParser(sourceText)
            .Parse()
            .Print(indented: false)
            .Replace("\r\n", "\n");

        // assert
        var stringValue = Assert.IsType<StringValueNode>(value);
        Assert.Equal("  x  ", stringValue.Value);
        Assert.Equal("f(a: \"\"\"\nx\n\"\"\")", printed);
    }

    [Fact]
    public void ParseBlockString_EscapedTripleQuote_DecodesToLiteralTripleQuote()
    {
        // arrange
        // \""" is the only escape that produces a triple-quote inside a block string.
        const string sourceText = "f(a: \"\"\"a\\\"\"\"b\"\"\")";

        // act
        var value = ParseArgumentValue(sourceText);

        // assert
        var stringValue = Assert.IsType<StringValueNode>(value);
        Assert.Equal("a\"\"\"b", stringValue.Value);
    }

    [Fact]
    public void ParseAndPrint_BlockStringWithEscapedTripleQuote_RoundTripsSemantically()
    {
        // arrange
        const string sourceText = "f(a: \"\"\"a\\\"\"\"b\"\"\")";
        var firstValue = ParseArgumentValue(sourceText);

        // act
        // the printer re-escapes the literal triple-quote as \""", so the value survives a reparse.
        var printed = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);
        var secondValue = ParseArgumentValue(printed);

        // assert
        var first = Assert.IsType<StringValueNode>(firstValue);
        var second = Assert.IsType<StringValueNode>(secondValue);
        Assert.Equal("a\"\"\"b", first.Value);
        Assert.Equal(first.Value, second.Value);
    }

    [Fact]
    public void ParseBlockString_InvalidQuoteEscape_ThrowsSyntaxException()
    {
        // arrange
        // a single \" is not a valid block string escape, only \""" is.
        const string sourceText = "f(a: \"\"\"a\\\"b\"\"\")";

        // act
        void Act() => new FieldSelectionMapParser(sourceText).Parse();

        // assert
        Assert.Equal(
            "Invalid character escape sequence: `\\\"`.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Theory]
    // Standard escapes are processed inside block strings just like in regular strings, matching
    // HotChocolate's Utf8Helper which routes block strings through the same escape decoder.
    [InlineData("a\\nb", "a\nb")]
    [InlineData("a\\u0041b", "aAb")]
    public void ParseBlockString_StandardEscape_DecodesLikeHotChocolate(
        string blockContent,
        string expectedValue)
    {
        // arrange
        var sourceText = $"f(a: \"\"\"{blockContent}\"\"\")";

        // act
        var value = ParseArgumentValue(sourceText);

        // assert
        var stringValue = Assert.IsType<StringValueNode>(value);
        Assert.Equal(expectedValue, stringValue.Value);
    }

    [Fact]
    public void HandBuiltStringValueNode_WithQuote_RoundTripsThroughPrintAndParse()
    {
        // arrange
        // a node built in code with an unescaped quote in its value.
        var node = new StringValueNode(null, "a\"b", false);

        // act
        var printed = $"f(a: {node})";
        var reparsed = (StringValueNode)ParseArgumentValue(printed);

        // assert
        Assert.Equal("f(a: \"a\\\"b\")", printed);
        Assert.Equal("a\"b", reparsed.Value);
    }

    private static IValueNode ParseArgumentValue(string sourceText)
    {
        var node = (PathNode)new FieldSelectionMapParser(sourceText).Parse();
        return node.PathSegment.Arguments[0].Value;
    }

    [Theory]
    // Parity batch ported from HotChocolate.Language so the FieldSelectionMap decoder produces
    // the same semantic value HC produces for the same input.

    // src/HotChocolate/Language/test/Language.Tests/Parser/Utf8HelperTests.cs
    // Unescape_StandardEscapeChars_OutputIsUnescaped
    [InlineData("f(a: \"hello_123_\\b\")", "hello_123_\b")]
    [InlineData("f(a: \"hello_123_\\f\")", "hello_123_\f")]
    [InlineData("f(a: \"hello_123_\\n\")", "hello_123_\n")]
    [InlineData("f(a: \"hello_123_\\r\")", "hello_123_\r")]
    [InlineData("f(a: \"hello_123_\\t\")", "hello_123_\t")]
    [InlineData("f(a: \"hello_123_\\\"\")", "hello_123_\"")]
    // Unescape_UnicodeEscapeChars_OutputIsUnescaped
    [InlineData("f(a: \"hello_123_\\u0024\")", "hello_123_$")]
    [InlineData("f(a: \"hello_123_\\u00A2\")", "hello_123_¢")]
    [InlineData("f(a: \"hello_123_\\u0939\")", "hello_123_ह")]
    [InlineData("f(a: \"hello_123_\\u20AC\")", "hello_123_€")]
    // Unescape_ForwardSlash / Unescape_BackslashEscape
    [InlineData("f(a: \"path\\/to\\/file\")", "path/to/file")]
    [InlineData("f(a: \"path\\\\to\\\\file\")", "path\\to\\file")]
    // Unescape_SurrogatePair_Emoji
    [InlineData("f(a: \"hello\\uD83D\\uDE00world\")", "hello😀world")]
    // Unescape_ConsecutiveEscapes
    [InlineData("f(a: \"\\n\\r\\t\\b\")", "\n\r\t\b")]
    // Unescape_UnicodeInLongString
    [InlineData(
        "f(a: \"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghij\\u20ACklmnopqrstuvwxyz0123456789\")",
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghij€klmnopqrstuvwxyz0123456789")]
    public void ParseStringValue_DecodesLikeHotChocolate_Matches(
        string sourceText,
        string expectedValue)
    {
        // arrange & act
        var value = ParseArgumentValue(sourceText);

        // assert
        var stringValue = Assert.IsType<StringValueNode>(value);
        Assert.Equal(expectedValue, stringValue.Value);
    }

    [Theory]
    // Block-string dedent parity ported from HotChocolate.Language.
    // src/HotChocolate/Language/test/Language.Tests/Parser/BlockStringHelperTests.cs
    // TrimLeadingEmptyLines
    [InlineData("\n\n\n\nblock string uses ", "block string uses ")]
    // NoTrimNeeded
    [InlineData("foo", "foo")]
    // TrimTrailingEmptyLines
    [InlineData("block string uses \n\n\n\n", "block string uses ")]
    // TrimCommonIndent
    [InlineData(
        "block string uses\n    block string uses",
        "block string uses\nblock string uses")]
    [InlineData(
        "    block string uses\n    block string uses",
        "    block string uses\nblock string uses")]
    [InlineData(
        "    block string uses\n\tblock string uses",
        "    block string uses\nblock string uses")]
    [InlineData(
        "block string uses\n    block string uses\n    block string uses",
        "block string uses\nblock string uses\nblock string uses")]
    // SingleLineSingleChar_Does_Not_Loop
    [InlineData(".", ".")]
    public void ParseBlockString_DedentsLikeHotChocolate_Matches(
        string blockContent,
        string expectedValue)
    {
        // arrange
        var sourceText = $"f(a: \"\"\"{blockContent}\"\"\")";

        // act
        var value = ParseArgumentValue(sourceText);

        // assert
        var stringValue = Assert.IsType<StringValueNode>(value);
        Assert.Equal(expectedValue, stringValue.Value);
    }

    [Theory]
    // variable in every position
    [InlineData("f(a: $var)")]
    [InlineData("f(a: [$x])")]
    [InlineData("f(a: { k: $x })")]
    [InlineData("field($x)")]
    // empty args
    [InlineData("f()")]
    // malformed
    [InlineData("f(a 1)")]
    [InlineData("f(a:)")]
    [InlineData("f(a: \"x)")]
    [InlineData("f(a: 01)")]
    [InlineData("f(a: 1.)")]
    [InlineData("f(a: 1")]
    public void Parse_InvalidArguments_ThrowsSyntaxException(string sourceText)
    {
        // arrange & act
        void Act() => new FieldSelectionMapParser(sourceText).Parse();

        // assert
        Assert.Throws<FieldSelectionMapSyntaxException>(Act);
    }

    [Fact]
    public void Parse_ArgumentWithVariableValue_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act() => new FieldSelectionMapParser("f(x: $var)").Parse();

        // assert
        Assert.Equal(
            "Unexpected character `$`.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Parse_EmptyArgumentList_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act() => new FieldSelectionMapParser("f()").Parse();

        // assert
        Assert.Equal(
            "Unexpected token: RightParenthesis.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Parse_ControlCharacterInString_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act() => new FieldSelectionMapParser("f(a: \"x\u0000y\")").Parse();

        // assert
        Assert.Equal(
            "Invalid character within string: `\u0000`.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Parse_ControlCharacterInBlockString_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act() => new FieldSelectionMapParser("f(a: \"\"\"x\u0000y\"\"\")").Parse();

        // assert
        Assert.Equal(
            "Invalid character within string: `\u0000`.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Parse_TrailingCommaInArguments_IsAllowed()
    {
        // arrange & act
        // commas are treated as insignificant whitespace by the lexer.
        var result = new FieldSelectionMapParser("f(a: 1,)").Parse().Print(indented: false);

        // assert
        Assert.Equal("f(a: 1)", result);
    }

    [Fact]
    public void Parse_WithNodeLimitExceeded_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var parser = new FieldSelectionMapParser(
                "field1.field2",
                new FieldSelectionMapParserOptions(maxAllowedNodes: 2));

            parser.Parse();
        }

        // assert
        Assert.Equal(
            "Source text contains more than 2 nodes. Parsing aborted.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Parse_PathSegmentFollowedByIntValue_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act() => new FieldSelectionMapParser("a.1").Parse();

        // assert
        Assert.Equal(
            "Expected a `Name`-token, but found a `IntValue`-token.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Parse_ArgumentValueNodesCountAgainstNodeLimit_ThrowsSyntaxException()
    {
        // arrange & act
        // "f(a: [1])" needs nine nodes; the extra nesting in "f(a: [[[1]]])" adds two list
        // value nodes, so a budget of ten is only exceeded by the nested argument value.
        static void Act()
        {
            var parser = new FieldSelectionMapParser(
                "f(a: [[[1]]])",
                new FieldSelectionMapParserOptions(maxAllowedNodes: 10));

            parser.Parse();
        }

        // assert
        Assert.Equal(
            "Source text contains more than 10 nodes. Parsing aborted.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }
}
