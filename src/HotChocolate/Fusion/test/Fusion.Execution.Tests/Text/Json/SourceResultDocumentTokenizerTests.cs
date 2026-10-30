using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Tests the rows and the input validation of the source JSON tokenizer, for documents and for the
/// elements of batched arrays. Each test parses its input in a single chunk and behind paddings that
/// place a chunk boundary at each position of the input, in each chunk layout, and asserts that every
/// parse creates the same rows.
/// </summary>
public partial class SourceResultDocumentTokenizerTests
{
    // Leading white space that makes every input span at least two data chunks.
    private const int Padding = 1100;

    private const string Rejected = "rejected";

    // The inputs that the mutation test mutates.
    private static readonly string[] s_mutationSeeds =
        [
            "{}",
            "[]",
            "{\"a\":1}",
            "[1,2,3]",
            "\"root\"",
            "123",
            "-0",
            "0",
            "-1.5e+10",
            "1E-2",
            "0.0",
            "true",
            "false",
            "null",
            " \t\r\n{ \"a\" : [ 1 , 2 ] , \"b\" : { } , \"c\" : [ ] } \n",
            "{\"a\":\"\\\"\\\\\\/\\b\\f\\n\\r\\t\\u00e9\\uD83D\\uDE00\"}",
            "{\"\\u0061\":1,\"a\\\"b\":2}",
            "{\"a\":\"\\uD800\",\"b\":\"\\uDC00x\"}",
            "[\"é\",\"日本\",\"😀\",\"\u007f\"]",
            "{\"a\":1}garbage",
            "{\"a\":1}}",
            "{\"a\":1} [",
            "[[[]]]",
            "[{},{}]",
            "{\"a\":[{\"b\":null,\"c\":[true,false]}],\"d\":{\"e\":{}}}",
            "[1.0e5,-0.5,123456789012345678901234567890,1e999,-0.0e-0]",
            "[\"" + new string('x', 5000) + "\",\"" + new string('y', 17) + "\\n" + new string('z', 40) + "\"]",
            "{\"data\":{\"user\":null},\"errors\":[{\"message\":\"x\",\"path\":[\"user\",0],\"extensions\":{\"code\":\"E\"}}]}",
            "[1] \r\n\t ",
            new string('[', 64) + new string(']', 64),
            Nest(64),
            "[\"" + new string('s', 20000) + "\"]",
            "{\"a\":\"" + string.Concat(Enumerable.Repeat("\\u00e9\\n", 1500)) + "\"}",
            "[" + new string('1', 3000) + ",-" + new string('2', 3000) + ".5e+7]",
            "[1E5,1e+5,1E-5,0e0,-0.0,0.5e-0,-123.456e-7]",
            "[1\t,\r\n2 ]",
            "{\"a\":1,\"a\":2}",
            "{\"\":\"\"}",
            "[\"\\/\",\"\\u0041\\u00E9\\uffff\",\"\\u004a\"]",
            "{}\u0000",
            "\"s\"\n",
            "true\t",
            "-1.5 "
        ];

    // The arrays that the batched mutation test mutates.
    private static readonly string[] s_arrayMutationSeeds =
        [
            "[]",
            " \t\r\n[ \n]",
            "[{}]",
            "[{\"data\":{\"a\":1}},{\"errors\":[{\"message\":\"x\"}]}]",
            "[1,\"a\",true,false,null,-1.5e3,0]",
            "[[],[1,[2]],{},[{}]]",
            "[ {\"a\" : [ 1 , 2 ] } , [ 3 ] , \"s\" ]",
            "[{\"a\":1}\n,\n{\"b\":2}\n]\n",
            "[\"\\u00e9\",{\"\\n\":\"\\\"\"},\"\\\\\"]",
            "[1]garbage",
            "[1]]",
            "[{}] [",
            "[{}],",
            "[]x",
            "[1] [2]",
            "[1]/**/",
            "[1]\u0000",
            "[\"" + new string('x', 5000) + "\",{\"b\":\"" + new string('y', 3000) + "\\n\"}]",
            "[" + new string('1', 3000) + ",-" + new string('2', 3000) + ".5e+7]",
            "[" + Nest(63) + "]",
            "[" + Nest(63) + "," + Nest(63) + "]",
            "[" + new string('[', 63) + new string(']', 63) + "]",
            "[" + string.Join(",", Enumerable.Repeat("{\"data\":{\"id\":\"1\"}}", 20)) + "]"
        ];

    [Fact]
    public void Parse_Should_CountPropertiesAndRows_When_InputIsObject()
    {
        // act
        var rows = Parse("{\"a\":1,\"b\":{\"c\":[true,false,null]},\"d\":[]}");

        // assert
        rows.MatchInlineSnapshot(
            """
            StartObject @0 properties=3 rows=16 complex
              PropertyName @1 "a"
              Number @5 1
              PropertyName @7 "b"
              StartObject @11 properties=1 rows=8 complex
                PropertyName @12 "c"
                StartArray @16 items=3 rows=5
                  True @17 true
                  False @22 false
                  Null @28 null
                EndArray @32
              EndObject @33
              PropertyName @35 "d"
              StartArray @39 items=0 rows=2
              EndArray @40
            EndObject @41
            """);
    }

    [Fact]
    public void Parse_Should_CountItemsAndRows_When_InputIsArray()
    {
        // act
        var rows = Parse("[1,[2,3],{},\"s\",[]]");

        // assert
        rows.MatchInlineSnapshot(
            """
            StartArray @0 items=5 rows=12 complex
              Number @1 1
              StartArray @3 items=2 rows=4
                Number @4 2
                Number @6 3
              EndArray @7
              StartObject @9 properties=0 rows=2
              EndObject @10
              String @12 "s"
              StartArray @16 items=0 rows=2
              EndArray @17
            EndArray @18
            """);
    }

    [Fact]
    public void Parse_Should_MarkStringsAsEscaped_When_StringsHaveEscapeSequences()
    {
        // act
        var rows = Parse(
            "{\"a\\\"b\":\"\\\"\\\\\\/\\b\\f\\n\\r\\t\",\"\\u0061\":\"\\u00e9\\uD83D\\uDE00\",\"c\":\"\\uD800\",\"plain\":\"x\"}");

        // assert
        rows.MatchInlineSnapshot(
            """
            StartObject @0 properties=4 rows=10
              PropertyName @1 "a\"b" escaped
              String @8 "\"\\\/\b\f\n\r\t" escaped
              PropertyName @27 "\u0061" escaped
              String @36 "\u00e9\uD83D\uDE00" escaped
              PropertyName @57 "c"
              String @61 "\uD800" escaped
              PropertyName @70 "plain"
              String @78 "x"
            EndObject @81
            """);
    }

    [Fact]
    public void Parse_Should_SkipWhiteSpace_When_WhiteSpaceSurroundsTokens()
    {
        // act
        var rows = Parse(" \t\r\n{ \"a\" : [ 1 , 2 ] , \"b\"\t\r\n:\n{ } } \n");

        // assert
        rows.MatchInlineSnapshot(
            """
            StartObject @4 properties=2 rows=10 complex
              PropertyName @6 "a"
              StartArray @12 items=2 rows=4
                Number @14 1
                Number @18 2
              EndArray @20
              PropertyName @24 "b"
              StartObject @32 properties=0 rows=2
              EndObject @34
            EndObject @36
            """);
    }

    [Fact]
    public void Parse_Should_ReadEveryNumberForm_When_ArrayHoldsNumbers()
    {
        // act
        var rows = Parse(
            "[0,-0,0.0,-0.0,1.5,1E5,1e+5,1E-5,0e0,0.5e-0,-123.456e-7,123456789012345678901234567890,1e999]");

        // assert
        rows.MatchInlineSnapshot(
            """
            StartArray @0 items=13 rows=15
              Number @1 0
              Number @3 -0
              Number @6 0.0
              Number @10 -0.0
              Number @15 1.5
              Number @19 1E5
              Number @23 1e+5
              Number @28 1E-5
              Number @33 0e0
              Number @37 0.5e-0
              Number @44 -123.456e-7
              Number @56 123456789012345678901234567890
              Number @87 1e999
            EndArray @92
            """);
    }

    [Fact]
    public void Parse_Should_CountLocationsInBytes_When_StringsHaveMultiByteCharacters()
    {
        // act
        var rows = Parse("[\"é\",\"日本\",\"😀\",\"\u007f\"]");

        // assert
        rows.MatchInlineSnapshot(
            """
            StartArray @0 items=4 rows=6
              String @1 "é"
              String @6 "日本"
              String @15 "😀"
              String @22 "\u007f"
            EndArray @25
            """);
    }

    [Fact]
    public void Parse_Should_KeepEveryProperty_When_PropertyNamesAreDuplicateOrEmpty()
    {
        // act
        var rows = Parse("{\"a\":1,\"a\":2,\"\":\"\"}");

        // assert
        rows.MatchInlineSnapshot(
            """
            StartObject @0 properties=3 rows=8
              PropertyName @1 "a"
              Number @5 1
              PropertyName @7 "a"
              Number @11 2
              PropertyName @13 ""
              String @16 ""
            EndObject @18
            """);
    }

    [Fact]
    public void Parse_Should_CreateRows_When_ValuesSpanSeveralChunks()
    {
        // act
        var rows = Parse(
            "{\"long\":\"" + new string('x', 5000) + "\","
            + "\"escaped\":\"" + string.Concat(Enumerable.Repeat("\\u00e9\\n", 1500)) + "\","
            + "\"numbers\":[" + new string('1', 3000) + ",-" + new string('2', 3000) + ".5e+7]}");

        // assert
        rows.MatchInlineSnapshot(
            """
            StartObject @0 properties=3 rows=11 complex
              PropertyName @1 "long"
              String @8 "xxxxxxxxxxxxxxx... (5002 bytes)
              PropertyName @5011 "escaped"
              String @5021 "\u00e9\n\u00e9\... (12002 bytes) escaped
              PropertyName @17024 "numbers"
              StartArray @17034 items=2 rows=4
                Number @17035 1111111111111111... (3000 bytes)
                Number @20036 -222222222222222... (3006 bytes)
              EndArray @23042
            EndObject @23043
            """);
    }

    [Fact]
    public void Parse_Should_CreateRows_When_InputIsGraphQLResponse()
    {
        // act
        var rows = Parse(
            "{\"data\":{\"user\":null},\"errors\":[{\"message\":\"x\",\"path\":[\"user\",0],"
            + "\"extensions\":{\"code\":\"E\"}}]}");

        // assert
        rows.MatchInlineSnapshot(
            """
            StartObject @0 properties=2 rows=24 complex
              PropertyName @1 "data"
              StartObject @8 properties=1 rows=4
                PropertyName @9 "user"
                Null @16 null
              EndObject @20
              PropertyName @22 "errors"
              StartArray @31 items=1 rows=16 complex
                StartObject @32 properties=3 rows=14 complex
                  PropertyName @33 "message"
                  String @43 "x"
                  PropertyName @47 "path"
                  StartArray @54 items=2 rows=4
                    String @55 "user"
                    Number @62 0
                  EndArray @63
                  PropertyName @65 "extensions"
                  StartObject @78 properties=1 rows=4
                    PropertyName @79 "code"
                    String @86 "E"
                  EndObject @89
                EndObject @90
              EndArray @91
            EndObject @92
            """);
    }

    [Theory]
    // a string
    [InlineData("\"root\"", "String @0 \"root\"")]
    // a number
    [InlineData("123", "Number @0 123")]
    // a negative zero
    [InlineData("-0", "Number @0 -0")]
    // a number with a fraction and an exponent
    [InlineData("-1.5e+10", "Number @0 -1.5e+10")]
    // a number with an upper case exponent
    [InlineData("1E-2", "Number @0 1E-2")]
    // the true literal
    [InlineData("true", "True @0 true")]
    // the false literal
    [InlineData("false", "False @0 false")]
    // the null literal
    [InlineData("null", "Null @0 null")]
    // a string followed by a line feed
    [InlineData("\"s\"\n", "String @0 \"s\"")]
    // a literal followed by a tab
    [InlineData("true\t", "True @0 true")]
    // a number followed by a space, which ends the number
    [InlineData("-1.5 ", "Number @0 -1.5")]
    // white space before the value is skipped
    [InlineData("  null", "Null @2 null")]
    public void Parse_Should_CreateOneRow_When_InputIsScalar(string input, string expected)
    {
        // act
        var rows = Parse(input);

        // assert
        Assert.Equal(expected, rows);
    }

    [Theory]
    // any text after the object
    [InlineData("garbage")]
    // a closing brace without an opening brace
    [InlineData("}")]
    // an array that is never closed
    [InlineData(" [")]
    // a NUL byte
    [InlineData("\u0000")]
    // a second object
    [InlineData("{\"b\":2}")]
    public void Parse_Should_StopAfterRootObject_When_InputContinues(string rest)
    {
        // act
        var rows = Parse("{\"a\":1}" + rest);

        // assert
        Assert.Equal(Parse("{\"a\":1}"), rows);
    }

    [Theory]
    // 64 nested arrays: a start and an end row each
    [InlineData(64, false, 128)]
    // 32 objects with one property and 32 arrays nested in turn: 3 rows per object, 2 rows per array and
    // 1 row for the innermost number
    [InlineData(64, true, 161)]
    public void Parse_Should_Accept_When_NestingIsAtMaxDepth(int depth, bool objects, int expectedRows)
    {
        // act
        var rows = Parse(objects ? Nest(depth) : new string('[', depth) + new string(']', depth));

        // assert
        Assert.Equal(expectedRows, rows.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Theory]
    [MemberData(nameof(TruncatedInputs))]
    public void Parse_Should_Reject_When_InputEndsEarly(string input)
    {
        // act
        var rows = Parse(input);

        // assert
        Assert.Equal(Rejected, rows);
    }

    public static TheoryData<string> TruncatedInputs()
        =>
        [
            // no input
            "",
            // only white space
            "   ",
            // an object without its closing brace
            "{",
            // an array without its closing bracket
            "[",
            // a property name without a colon
            "{\"a\"",
            // a property name without a value
            "{\"a\":",
            // a property without the closing brace
            "{\"a\":1",
            // a comma without the next property
            "{\"a\":1,",
            // a string that ends inside an escape sequence
            "[\"a\\",
            // a string that ends inside a unicode escape sequence
            "[\"a\\u00",
            // a string without its closing quote
            "[\"unterminated]",
            "\"abc",
            "\"",
            // a long string without its closing quote
            "[\"" + new string('s', 20000),
            // a number that ends after its decimal point
            "1.",
            "[" + new string('1', 3000) + ".",
            // a number that ends after its minus sign
            "-",
            // a number that ends after its exponent sign
            "1e+"
        ];

    [Theory]
    [MemberData(nameof(InputsWithInvalidSeparators))]
    public void Parse_Should_Reject_When_SeparatorIsMissingOrMisplaced(string input)
    {
        // act
        var rows = Parse(input);

        // assert
        Assert.Equal(Rejected, rows);
    }

    public static TheoryData<string> InputsWithInvalidSeparators()
        =>
        [
            // a comma after the last item
            "[1,]",
            "[1,\n]",
            // a comma after the last property
            "{\"a\":1,}",
            // a comma before the first item
            "[,1]",
            // a comma in an empty object
            "{,}",
            // two commas
            "[1,,2]",
            // no comma between items
            "[1 2]",
            // no comma between properties
            "{\"a\":1 \"b\":2}",
            // no colon after a property name
            "{\"a\" 1}",
            // two colons
            "{\"a\"::1}",
            // a comma instead of a colon
            "{\"a\",1}",
            // a colon without a value
            "{\"a\":}",
            // a property name without a colon and value
            "{\"a\":1,\"b\"}"
        ];

    [Theory]
    [MemberData(nameof(InputsWithInvalidStructure))]
    public void Parse_Should_Reject_When_StructureIsInvalid(string input)
    {
        // act
        var rows = Parse(input);

        // assert
        Assert.Equal(Rejected, rows);
    }

    public static TheoryData<string> InputsWithInvalidStructure()
        =>
        [
            // an array closed with a brace
            "[1}",
            // an object closed with a bracket
            "{\"a\":1]",
            // a closing bracket without an opening bracket
            "]",
            // a closing brace without an opening brace
            "}",
            // a colon without a property
            ":",
            // a comma without a container
            ",",
            // a property name without quotes
            "{a:1}",
            // a property name in single quotes
            "{'a':1}",
            // a number as property name
            "{1:2}"
        ];

    [Theory]
    [MemberData(nameof(InputsWithContentAfterRootValue))]
    public void Parse_Should_Reject_When_ContentFollowsRootArrayOrScalar(string input)
    {
        // act
        var rows = Parse(input);

        // assert
        Assert.Equal(Rejected, rows);
    }

    public static TheoryData<string> InputsWithContentAfterRootValue()
        =>
        [
            // text after a root array
            "[1]x",
            // a second root array
            "[1] [2]",
            // a comment after a root array
            "[1]/**/",
            // a second root number
            "1 2",
            // a second root string
            "\"a\" \"b\"",
            // a second root literal
            "truefalse"
        ];

    [Theory]
    [MemberData(nameof(InvalidNumbers))]
    public void Parse_Should_Reject_When_NumberIsInvalid(string input)
    {
        // act
        var rows = Parse(input);

        // assert
        Assert.Equal(Rejected, rows);
    }

    public static TheoryData<string> InvalidNumbers()
        =>
        [
            // a leading zero
            "[01]",
            "[-01]",
            "[00]",
            "-00",
            "[01.5]",
            // a minus sign without digits
            "[-]",
            "[-a]",
            "[--1]",
            // a decimal point without digits
            "[1.]",
            "[.5]",
            "[.]",
            // an exponent without digits
            "[1e]",
            "[1e+]",
            "[1.5E-]",
            "[1e+-5]",
            // a plus sign
            "[+1]",
            // a hexadecimal number
            "[0x10]",
            "0x",
            // characters directly after a number
            "[1x]",
            "[1/]",
            "[1-2]",
            "[1.0.0]",
            "[1e5.5]",
            // names that are not JSON numbers
            "[NaN]",
            "[Infinity]",
            "[-Infinity]"
        ];

    [Theory]
    [MemberData(nameof(InvalidLiterals))]
    public void Parse_Should_Reject_When_LiteralIsInvalid(string input)
    {
        // act
        var rows = Parse(input);

        // assert
        Assert.Equal(Rejected, rows);
    }

    public static TheoryData<string> InvalidLiterals()
        =>
        [
            // a literal that is cut short
            "[tru]",
            "[nul]",
            // a literal with upper case letters
            "[True]",
            "tRue",
            // characters directly after a literal
            "[nulll]",
            "[truex]"
        ];

    [Theory]
    [MemberData(nameof(InvalidStrings))]
    public void Parse_Should_Reject_When_StringIsInvalid(string input)
    {
        // act
        var rows = Parse(input);

        // assert
        Assert.Equal(Rejected, rows);
    }

    public static TheoryData<string> InvalidStrings()
        =>
        [
            // a string in single quotes
            "['a']",
            // an unknown escape sequence
            "[\"a\\x\"]",
            // an upper case unicode escape sequence
            "[\"\\U0041\"]",
            // a unicode escape sequence with too few hex digits
            "[\"a\\u12\"]",
            "{\"a\":\"" + string.Concat(Enumerable.Repeat("\\u00e9", 1500)) + "\\u00\"}",
            // a unicode escape sequence with a character that is not a hex digit
            "[\"a\\u12G4\"]",
            // an escaped closing quote
            "[\"\\\"]",
            // an unescaped tab
            "[\"tab\there\"]",
            // an unescaped line feed
            "[\"a\nb\"]",
            // an unescaped NUL
            "[\"a\u0000b\"]",
            // an unescaped unit separator, the last control character
            "[\"\u001f\"]"
        ];

    [Theory]
    [MemberData(nameof(InputsWithInvalidWhiteSpace))]
    public void Parse_Should_Reject_When_InputHasCommentsOrOtherWhiteSpace(string input)
    {
        // act
        var rows = Parse(input);

        // assert
        Assert.Equal(Rejected, rows);
    }

    public static TheoryData<string> InputsWithInvalidWhiteSpace()
        =>
        [
            // a line comment
            "// c\n{}",
            // a block comment
            "/* c */{}",
            // a form feed
            "[1\f]",
            // a vertical tab
            "[1\v]",
            // a no-break space
            "[\u00a01]",
            // a byte order mark
            "\uFEFF{}",
            // a letter outside a string
            "\u00e9"
        ];

    [Theory]
    // 65 nested arrays
    [InlineData(65, false)]
    // 65 nested objects and arrays
    [InlineData(65, true)]
    public void Parse_Should_Reject_When_NestingExceedsMaxDepth(int depth, bool objects)
    {
        // act
        var rows = Parse(objects ? Nest(depth) : new string('[', depth) + new string(']', depth));

        // assert
        Assert.Equal(Rejected, rows);
    }

    [Theory]
    // ["<FF>"]: 0xFF never occurs in UTF-8.
    [InlineData(new byte[] { 0x5B, 0x22, 0xFF, 0x22, 0x5D }, true)]
    // ["<C3>"]: a two-byte lead byte without its continuation byte.
    [InlineData(new byte[] { 0x5B, 0x22, 0xC3, 0x22, 0x5D }, true)]
    // ["<C0 AF>"]: an overlong encoding of '/'.
    [InlineData(new byte[] { 0x5B, 0x22, 0xC0, 0xAF, 0x22, 0x5D }, true)]
    // ["<ED A0 80>"]: the surrogate U+D800 encoded as UTF-8.
    [InlineData(new byte[] { 0x5B, 0x22, 0xED, 0xA0, 0x80, 0x22, 0x5D }, true)]
    // [<FF>]: outside a string 0xFF is not the start of any JSON value.
    [InlineData(new byte[] { 0x5B, 0xFF, 0x5D }, false)]
    // <EF BB BF>{}: the reader does not skip a UTF-8 byte order mark, so 0xEF is an unexpected byte.
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x7B, 0x7D }, false)]
    public void Parse_Should_ValidateOnlyStructure_When_InputIsNotValidUtf8(byte[] input, bool accepted)
    {
        // act
        var rows = AssertBoundaryIndependence(input);

        // assert
        Assert.Equal(accepted, rows != Rejected);
    }

    [Fact]
    public void Parse_Should_CreateSameRows_When_MutatedInputChunkBoundariesMove()
    {
        // arrange
        var corpus = CreateCorpus();
        var random = new Random(57);
        var accepted = 0;
        var rejected = 0;

        // act
        for (var i = 0; i < 4000; i++)
        {
            var input = Mutate(random, corpus[random.Next(corpus.Length)]);

            if (AssertBoundaryIndependence(input, RandomBoundaryPaddings(random, input.Length)) == Rejected)
            {
                rejected++;
            }
            else
            {
                accepted++;
            }
        }

        // assert
        // The counts pin which of the mutated inputs are accepted.
        Assert.Equal(717, accepted);
        Assert.Equal(3283, rejected);
    }

    [Fact]
    public void ParseArrayElements_Should_CreateOneDocumentPerElement_When_ElementsAreObjects()
    {
        // act
        var elements = ParseArrayElements("[{\"data\":{\"a\":1}},{\"errors\":[{\"message\":\"x\"}]}]");

        // assert
        elements.MatchInlineSnapshot(
            """
            Element 0
              StartObject @1 properties=1 rows=7 complex
                PropertyName @2 "data"
                StartObject @9 properties=1 rows=4
                  PropertyName @10 "a"
                  Number @14 1
                EndObject @15
              EndObject @16
            Element 1
              StartObject @18 properties=1 rows=9 complex
                PropertyName @19 "errors"
                StartArray @28 items=1 rows=6 complex
                  StartObject @29 properties=1 rows=4
                    PropertyName @30 "message"
                    String @40 "x"
                  EndObject @43
                EndArray @44
              EndObject @45
            """);
    }

    [Fact]
    public void ParseArrayElements_Should_CreateOneDocumentPerElement_When_ElementsAreScalarsOrArrays()
    {
        // act
        var elements = ParseArrayElements("[1,\"a\\n\",true,false,null,-1.5e3,[],[1,[2]],{}]");

        // assert
        elements.MatchInlineSnapshot(
            """
            Element 0
              Number @1 1
            Element 1
              String @3 "a\n" escaped
            Element 2
              True @9 true
            Element 3
              False @14 false
            Element 4
              Null @20 null
            Element 5
              Number @25 -1.5e3
            Element 6
              StartArray @32 items=0 rows=2
              EndArray @33
            Element 7
              StartArray @35 items=2 rows=6 complex
                Number @36 1
                StartArray @38 items=1 rows=3
                  Number @39 2
                EndArray @40
              EndArray @41
            Element 8
              StartObject @43 properties=0 rows=2
              EndObject @44
            """);
    }

    [Fact]
    public void ParseArrayElements_Should_SkipWhiteSpace_When_WhiteSpaceSurroundsSeparators()
    {
        // act
        var elements = ParseArrayElements(" \t\r\n[ {\"a\" : [ 1 , 2 ] }\n,\n[ 3 ] , \"s\" ]\n");

        // assert
        elements.MatchInlineSnapshot(
            """
            Element 0
              StartObject @6 properties=1 rows=7 complex
                PropertyName @7 "a"
                StartArray @13 items=2 rows=4
                  Number @15 1
                  Number @19 2
                EndArray @21
              EndObject @23
            Element 1
              StartArray @27 items=1 rows=3
                Number @29 3
              EndArray @31
            Element 2
              String @35 "s"
            """);
    }

    [Fact]
    public void ParseArrayElements_Should_CreateRows_When_ElementsSpanSeveralChunks()
    {
        // act
        var elements = ParseArrayElements(
            "[\"" + new string('x', 5000) + "\","
            + "{\"b\":\"" + new string('y', 3000) + "\\n\"},"
            + new string('1', 3000) + "]");

        // assert
        elements.MatchInlineSnapshot(
            """
            Element 0
              String @1 "xxxxxxxxxxxxxxx... (5002 bytes)
            Element 1
              StartObject @5004 properties=1 rows=4
                PropertyName @5005 "b"
                String @5009 "yyyyyyyyyyyyyyy... (3004 bytes) escaped
              EndObject @8013
            Element 2
              Number @8015 1111111111111111... (3000 bytes)
            """);
    }

    [Theory]
    // an empty array
    [InlineData("[]")]
    // an empty array with white space around and inside it
    [InlineData(" \t\r\n[ \n]")]
    public void ParseArrayElements_Should_CreateNoDocuments_When_ArrayIsEmpty(string input)
    {
        // act
        var elements = ParseArrayElements(input);

        // assert
        Assert.Equal("", elements);
    }

    [Theory]
    // any text after the array
    [InlineData("garbage")]
    // a closing bracket without an opening bracket
    [InlineData("]")]
    // a comma
    [InlineData(",")]
    // an array that is never closed
    [InlineData(" [")]
    // a second array
    [InlineData(" [2]")]
    // a comment
    [InlineData("/**/")]
    // a NUL byte
    [InlineData("\u0000")]
    public void ParseArrayElements_Should_StopAfterArray_When_InputContinues(string rest)
    {
        // act
        var elements = ParseArrayElements("[1,{}]" + rest);

        // assert
        Assert.Equal(ParseArrayElements("[1,{}]"), elements);
    }

    [Theory]
    [MemberData(nameof(ArraysAtMaxDepth))]
    public void ParseArrayElements_Should_Accept_When_ElementNestingIsAtMaxDepth(string input, int expectedElements)
    {
        // act
        var elements = ParseArrayElements(input);

        // assert
        Assert.Equal(expectedElements, elements.Split('\n').Count(t => t.StartsWith("Element ")));
    }

    public static TheoryData<string, int> ArraysAtMaxDepth()
        => new()
        {
            // one element of 63 nested arrays, 64 levels with the batched array
            { "[" + new string('[', 63) + new string(']', 63) + "]", 1 },
            // two elements of 63 nested objects and arrays
            { "[" + Nest(63) + "," + Nest(63) + "]", 2 },
            // a flat element before an element of 63 nested arrays
            { "[{}," + new string('[', 63) + new string(']', 63) + "]", 2 }
        };

    [Theory]
    [MemberData(nameof(ArraysOverMaxDepth))]
    public void ParseArrayElements_Should_Reject_When_ElementNestingExceedsMaxDepth(string input)
    {
        // act
        var elements = ParseArrayElements(input);

        // assert
        Assert.Equal(Rejected, elements);
    }

    public static TheoryData<string> ArraysOverMaxDepth()
        =>
        [
            // one element of 64 nested arrays, 65 levels with the batched array
            "[" + new string('[', 64) + new string(']', 64) + "]",
            // one element of 64 nested objects and arrays
            "[" + Nest(64) + "]",
            // a flat element before an element of 64 nested objects and arrays
            "[{}," + Nest(64) + "]"
        ];

    [Theory]
    [MemberData(nameof(TruncatedArrays))]
    public void ParseArrayElements_Should_Reject_When_ArrayEndsEarly(string input)
    {
        // act
        var elements = ParseArrayElements(input);

        // assert
        Assert.Equal(Rejected, elements);
    }

    public static TheoryData<string> TruncatedArrays()
        =>
        [
            // an opening bracket only
            "[",
            "[ \n",
            // an element without the closing bracket
            "[{}",
            "[{} ",
            // a comma without the next element
            "[{},",
            // a number at the end of the input
            "[1",
            "[" + new string('1', 3000),
            // a number that ends after its decimal point
            "[1.",
            // a number that ends after its minus sign
            "[-",
            // a literal at the end of the input
            "[true",
            // a string without its closing quote
            "[\"abc",
            "[\"" + new string('s', 20000),
            // a string that ends inside an escape sequence
            "[\"a\\"
        ];

    [Theory]
    [MemberData(nameof(ArraysWithInvalidSeparators))]
    public void ParseArrayElements_Should_Reject_When_SeparatorIsMissingOrMisplaced(string input)
    {
        // act
        var elements = ParseArrayElements(input);

        // assert
        Assert.Equal(Rejected, elements);
    }

    public static TheoryData<string> ArraysWithInvalidSeparators()
        =>
        [
            // a comma after the last element
            "[{},]",
            "[1,]",
            // a comma before the first element
            "[,{}]",
            // a comma without elements
            "[,]",
            // two commas
            "[{},,{}]",
            // no comma between objects
            "[{} {}]",
            "[{}{}]",
            // no comma between scalars
            "[1 2]",
            "[\"a\" \"b\"]",
            // a colon between elements
            "[{}:{}]"
        ];

    [Theory]
    [MemberData(nameof(ArraysWithInvalidStructure))]
    public void ParseArrayElements_Should_Reject_When_StructureIsInvalid(string input)
    {
        // act
        var elements = ParseArrayElements(input);

        // assert
        Assert.Equal(Rejected, elements);
    }

    public static TheoryData<string> ArraysWithInvalidStructure()
        =>
        [
            // a closing brace after an element object
            "[{}}",
            // the batched array closed with a brace
            "[1}",
            // an element object closed with a bracket
            "[{]",
            "[{\"a\":1]",
            // a literal that is cut short
            "[tru]",
            // a number with a leading zero
            "[{\"a\":01}]"
        ];

    [Theory]
    [MemberData(nameof(ArraysWithInvalidWhiteSpace))]
    public void ParseArrayElements_Should_Reject_When_ArrayHasCommentsOrOtherWhiteSpace(string input)
    {
        // act
        var elements = ParseArrayElements(input);

        // assert
        Assert.Equal(Rejected, elements);
    }

    public static TheoryData<string> ArraysWithInvalidWhiteSpace()
        =>
        [
            // a block comment after an element
            "[{}/**/]",
            // a line comment before an element
            "[//c\n1]",
            // a form feed after an element
            "[1\f]",
            // a vertical tab before an element
            "[\v1]",
            // a no-break space after an element
            "[{}\u00a0]",
            // a byte order mark before an element
            "[\uFEFF1]"
        ];

    [Fact]
    public void ParseArrayElements_Should_CreateSameRows_When_MutatedArrayChunkBoundariesMove()
    {
        // arrange
        var corpus = CreateArrayCorpus();
        var random = new Random(91);
        var accepted = 0;
        var rejected = 0;

        // act
        while (accepted + rejected < 3000)
        {
            var input = Mutate(random, corpus[random.Next(corpus.Length)]);

            // A batched response body starts with an array.
            if (!StartsWithArray(input))
            {
                continue;
            }

            if (AssertArrayBoundaryIndependence(input, RandomBoundaryPaddings(random, input.Length)) == Rejected)
            {
                rejected++;
            }
            else
            {
                accepted++;
            }
        }

        // assert
        // The counts pin which of the mutated arrays are accepted.
        Assert.Equal(698, accepted);
        Assert.Equal(2302, rejected);
    }

    /// <summary>
    /// Parses <paramref name="input"/> in each chunk layout and returns the rows that every parse
    /// creates, or <see cref="Rejected"/> when every parse rejects it.
    /// </summary>
    private static string Parse(string input)
        => AssertBoundaryIndependence(Encoding.UTF8.GetBytes(input));

    private static string AssertBoundaryIndependence(byte[] input)
        => AssertBoundaryIndependence(input, GetBoundaryPaddings(input.Length));

    /// <summary>
    /// Parses <paramref name="input"/> behind each padding of <paramref name="paddings"/> in each chunk
    /// layout, asserts that each parse creates the rows of the input in a single chunk, and returns those
    /// rows, or <see cref="Rejected"/> when the input is rejected.
    /// </summary>
    private static string AssertBoundaryIndependence(byte[] input, IEnumerable<int> paddings)
    {
        var expected = ParseUnpadded(input);
        var results = new StringBuilder();
        var expectedResults = new StringBuilder();

        foreach (var padding in paddings)
        {
            var padded = Pad(input, padding);

            foreach (var layout in Enum.GetValues<ChunkLayout>())
            {
                results.Append($"{layout} {padding}\n{ParseFilled(padded, padding, layout)}\n");
                expectedResults.Append($"{layout} {padding}\n{expected}\n");
            }
        }

        Assert.Equal(expectedResults.ToString(), results.ToString());
        return expected;
    }

    /// <summary>
    /// Parses the elements of the array in <paramref name="input"/> in each chunk layout and returns the
    /// rows that every parse creates, or <see cref="Rejected"/> when every parse rejects it.
    /// </summary>
    private static string ParseArrayElements(string input)
        => AssertArrayBoundaryIndependence(Encoding.UTF8.GetBytes(input), GetBoundaryPaddings(input.Length));

    /// <summary>
    /// Parses the elements of the array in <paramref name="input"/> behind each padding of
    /// <paramref name="paddings"/> in each chunk layout, asserts that each parse creates the rows of the
    /// input in adjacent chunks, and returns those rows, or <see cref="Rejected"/> when the array is
    /// rejected.
    /// </summary>
    private static string AssertArrayBoundaryIndependence(byte[] input, IEnumerable<int> paddings)
    {
        var expected = ParseArrayElements(input, 0, ChunkLayout.Adjacent);
        var results = new StringBuilder();
        var expectedResults = new StringBuilder();

        foreach (var padding in paddings)
        {
            var padded = Pad(input, padding);

            foreach (var layout in Enum.GetValues<ChunkLayout>())
            {
                results.Append($"{layout} {padding}\n{ParseArrayElements(padded, padding, layout)}\n");
                expectedResults.Append($"{layout} {padding}\n{expected}\n");
            }
        }

        Assert.Equal(expectedResults.ToString(), results.ToString());
        return expected;
    }

    private static string ParseArrayElements(byte[] input, int padding, ChunkLayout layout)
    {
        using var arena = new MemoryArena();
        var (segments, usedChunks, lastLength) = CreateSegments(input, layout);
        List<SourceResultDocument> documents = [];

        try
        {
            documents = SourceResultDocument.ParseFilledArrayElements(arena, segments, usedChunks, lastLength);
            var text = new StringBuilder();

            for (var i = 0; i < documents.Count; i++)
            {
                text.Append(i > 0 ? "\n" : "").Append("Element ").Append(i);

                foreach (var line in DescribeRows(documents[i], input, padding).Split('\n'))
                {
                    text.Append("\n  ").Append(line);
                }
            }

            return text.ToString();
        }
        catch (JsonException)
        {
            return Rejected;
        }
        finally
        {
            foreach (var document in documents)
            {
                document.Dispose();
            }

            arena.Seal();
        }
    }

    private static bool StartsWithArray(byte[] input)
    {
        foreach (var value in input)
        {
            if (value is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n'))
            {
                return value == (byte)'[';
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the paddings that place the end of the first data chunk at each position of an input of
    /// <paramref name="length"/> bytes, sampled for long inputs.
    /// </summary>
    private static IEnumerable<int> GetBoundaryPaddings(int length)
    {
        var boundary = SourceResultDocument.GetDataChunkSize(0);
        yield return Padding;

        for (var position = 0; position <= length; position++)
        {
            if (length <= 80 || position <= 40 || position >= length - 40 || position % 37 == 0)
            {
                yield return boundary - Math.Min(position, boundary);
            }
        }
    }

    private static int[] RandomBoundaryPaddings(Random random, int length)
    {
        var boundary = SourceResultDocument.GetDataChunkSize(0);
        return
        [
            Padding,
            boundary - random.Next(Math.Min(length, boundary) + 1),
            boundary - random.Next(Math.Min(length, boundary) + 1)
        ];
    }

    private static byte[] Pad(byte[] input, int padding)
    {
        var padded = new byte[padding + input.Length];
        padded.AsSpan(0, padding).Fill((byte)' ');
        input.CopyTo(padded, padding);
        return padded;
    }

    private static string ParseFilled(byte[] input, int padding, ChunkLayout layout)
    {
        using var arena = new MemoryArena();
        var (segments, usedChunks, lastLength) = CreateSegments(input, layout);

        try
        {
            using var document = SourceResultDocument.ParseFilled(arena, segments, usedChunks, lastLength);
            return DescribeRows(document, input, padding);
        }
        catch (JsonException)
        {
            return Rejected;
        }
        finally
        {
            arena.Seal();
        }
    }

    private static (MemorySegment[] Segments, int UsedChunks, int LastLength) CreateSegments(
        byte[] input,
        ChunkLayout layout)
    {
        var segments = new MemorySegment[64];
        var backing = new byte[(input.Length * 2) + 4096];
        var pair = Array.Empty<byte>();
        var offset = 0;
        var consumed = 0;
        var usedChunks = 0;
        var lastLength = 0;

        while (consumed < input.Length)
        {
            var chunkSize = SourceResultDocument.GetDataChunkSize(usedChunks);
            MemorySegment segment;

            switch (layout)
            {
                case ChunkLayout.Adjacent:
                    segment = new MemorySegment(backing, offset, chunkSize);
                    break;

                case ChunkLayout.Separate:
                    segment = new MemorySegment(new byte[chunkSize], 0, chunkSize);
                    break;

                default:
                    if (usedChunks % 2 == 0)
                    {
                        pair = new byte[chunkSize + SourceResultDocument.GetDataChunkSize(usedChunks + 1)];
                        segment = new MemorySegment(pair, 0, chunkSize);
                    }
                    else
                    {
                        segment = new MemorySegment(pair, pair.Length - chunkSize, chunkSize);
                    }

                    break;
            }

            segments[usedChunks++] = segment;
            lastLength = Math.Min(chunkSize, input.Length - consumed);
            input.AsSpan(consumed, lastLength).CopyTo(segment.Span);
            consumed += lastLength;
            offset += chunkSize;
        }

        return (segments, usedChunks, lastLength);
    }

    private static string ParseUnpadded(byte[] input)
    {
        using var arena = new MemoryArena();

        try
        {
            using var document = SourceResultDocument.Parse(arena, input, input.Length);
            return DescribeRows(document, input, 0);
        }
        catch (JsonException)
        {
            return Rejected;
        }
        finally
        {
            arena.Seal();
        }
    }

    /// <summary>
    /// Describes the rows of <paramref name="document"/>, one line per row indented by nesting depth,
    /// with the bytes of <paramref name="input"/> that each value row covers. Locations are reported
    /// relative to <paramref name="padding"/>.
    /// </summary>
    private static string DescribeRows(SourceResultDocument document, byte[] input, int padding)
    {
        var text = new StringBuilder();
        var starts = new Stack<SourceResultDocument.DbRow>();
        var cursor = SourceResultDocument.Cursor.Zero;
        var rowCount = document.GetDbRow(cursor).NumberOfRows;

        for (var i = 0; i < rowCount; i++, cursor++)
        {
            var row = document.GetDbRow(cursor);
            var location = (int)SourceResultDocument.PackedToLinear(row.Location);

            switch (row.TokenType)
            {
                case JsonTokenType.StartObject:
                case JsonTokenType.StartArray:
                    text.Append(' ', starts.Count * 2)
                        .Append($"{row.TokenType} @{location - padding} ")
                        .Append(row.TokenType == JsonTokenType.StartObject ? "properties=" : "items=")
                        .Append($"{row.SizeOrLength} rows={row.NumberOfRows}")
                        .Append(row.HasComplexChildren ? " complex" : "")
                        .Append('\n');
                    starts.Push(row);
                    break;

                case JsonTokenType.EndObject:
                case JsonTokenType.EndArray:
                    var start = starts.Pop();
                    text.Append(' ', starts.Count * 2).Append($"{row.TokenType} @{location - padding}");

                    // The end row repeats the counts of the start row.
                    if (row.SizeOrLength != start.SizeOrLength || row.NumberOfRows != start.NumberOfRows)
                    {
                        text.Append($" mismatch size={row.SizeOrLength} rows={row.NumberOfRows}");
                    }

                    text.Append('\n');
                    break;

                default:
                    text.Append(' ', starts.Count * 2)
                        .Append($"{row.TokenType} @{location - padding} ")
                        .Append(DescribeBytes(input.AsSpan(location, row.SizeOrLength)))
                        .Append(row.HasComplexChildren ? " escaped" : "")
                        .Append('\n');
                    break;
            }
        }

        return text.ToString().TrimEnd('\n');
    }

    /// <summary>
    /// Describes the bytes of a value, shortened when it has more than 40 bytes.
    /// </summary>
    private static string DescribeBytes(ReadOnlySpan<byte> value)
    {
        var text = new StringBuilder();

        foreach (var c in Encoding.UTF8.GetString(value.Length > 40 ? value[..16] : value))
        {
            text.Append(
                c switch
                {
                    '\t' => "\\t",
                    '\r' => "\\r",
                    '\n' => "\\n",
                    < ' ' or '\u007f' => $"\\u{(int)c:x4}",
                    _ => c.ToString()
                });
        }

        return value.Length > 40 ? text.Append($"... ({value.Length} bytes)").ToString() : text.ToString();
    }

    private static byte[][] CreateCorpus()
    {
        var corpus = new List<byte[]>();

        foreach (var input in s_mutationSeeds)
        {
            corpus.Add(Encoding.UTF8.GetBytes(input));
        }

        corpus.Add(Encoding.UTF8.GetBytes(CreateReviewsResponse(users: 2, compact: true)));
        corpus.Add(Encoding.UTF8.GetBytes(CreateReviewsResponse(users: 1, compact: false)));
        corpus.Add(
            Encoding.UTF8.GetBytes(
                "{\"requestIndex\":1,\"variableIndex\":3,\"data\":{\"productByUpc\":"
                + "{\"inStock\":true,\"shippingEstimate\":-12.5e-3,\"name\":\"T\\u00e9st \\\"1\\\"\"}}}"));
        return [.. corpus];
    }

    private static byte[][] CreateArrayCorpus()
    {
        var corpus = new List<byte[]>();

        foreach (var input in s_arrayMutationSeeds)
        {
            corpus.Add(Encoding.UTF8.GetBytes(input));
        }

        var records = CreateCorpus().Select(Encoding.UTF8.GetString).ToArray();
        corpus.Add(Encoding.UTF8.GetBytes("[" + string.Join(",", records.Take(4)) + "]"));
        corpus.Add(Encoding.UTF8.GetBytes("[ " + string.Join(" ,\n ", records.Skip(20)) + " ]"));
        corpus.Add(Encoding.UTF8.GetBytes("[" + string.Join(",", records.Where(t => t.StartsWith('{'))) + "]"));
        return [.. corpus];
    }

    private static string CreateReviewsResponse(int users, bool compact)
    {
        var separator = compact ? "" : "\n  ";
        var text = new StringBuilder("{" + separator + "\"data\":{\"_0__entities\":[");

        for (var u = 0; u < users; u++)
        {
            text.Append(u > 0 ? "," : "").Append(separator).Append("{\"reviews\":[");

            for (var r = 0; r < 2; r++)
            {
                text.Append(r > 0 ? "," : "")
                    .Append("{\"id\":\"").Append(r + 1)
                    .Append("\",\"body\":\"Lorem ipsum dolor sit amet, \\\"consectetur\\\" adipiscing elit.\",")
                    .Append(separator)
                    .Append("\"product\":{\"upc\":\"1\",\"price\":899,\"weight\":1.25,\"inStock\":false,\"x\":null}}");
            }

            text.Append("]}");
        }

        return text.Append("]}}").ToString();
    }

    private static byte[] Mutate(Random random, byte[] input)
    {
        var interesting = "{}[],:\"\\ \t\n\r0123456789-+.eEtrufalsnu/\u007f"u8;
        var bytes = new List<byte>(input);
        var mutations = random.Next(1, 4);

        for (var i = 0; i < mutations; i++)
        {
            var position = bytes.Count == 0 ? 0 : random.Next(bytes.Count);
            var value = random.Next(4) == 0
                ? (byte)random.Next(256)
                : interesting[random.Next(interesting.Length)];

            switch (random.Next(6))
            {
                case 0 when bytes.Count > 0:
                    bytes[position] = value;
                    break;

                case 1:
                    bytes.Insert(position, value);
                    break;

                case 2 when bytes.Count > 0:
                    bytes.RemoveRange(position, Math.Min(random.Next(1, 4), bytes.Count - position));
                    break;

                case 3:
                    bytes.RemoveRange(position, bytes.Count - position);
                    break;

                case 4 when bytes.Count > 0:
                    var length = Math.Min(random.Next(1, 16), bytes.Count - position);
                    bytes.InsertRange(random.Next(bytes.Count), bytes.GetRange(position, length));
                    break;

                default:
                    if (bytes.Count > 1)
                    {
                        var other = random.Next(bytes.Count);
                        (bytes[position], bytes[other]) = (bytes[other], bytes[position]);
                    }

                    break;
            }
        }

        return [.. bytes];
    }

    private static string Nest(int depth)
    {
        var text = new StringBuilder();

        for (var i = 0; i < depth; i++)
        {
            text.Append(i % 2 == 0 ? "{\"a\":" : "[");
        }

        text.Append('1');

        for (var i = depth - 1; i >= 0; i--)
        {
            text.Append(i % 2 == 0 ? "}" : "]");
        }

        return text.ToString();
    }

    private enum ChunkLayout
    {
        Adjacent,
        Separate,
        Paired
    }
}
