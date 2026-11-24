namespace HotChocolate.Text.Yaml;

public class YamlDocumentTests
{
    [Fact]
    public void Parse_Should_ReadNestedObjectsAndArrays_When_DocumentUsesBlockStyle()
    {
        // arrange
        const string yaml =
            """
            # gateway config
            planner:
              enableRequestGrouping: false # inline comment
              maxPlanningTime: 00:00:05
            sources:
              - name: accounts
                url: http://accounts/graphql
              - name: "reviews"
                headers:
                  - Authorization
                  - X-Request-Id
            tags:
            - a
            - b
            empty: []
            none: {}
            missing:
            """;

        // act
        var document = YamlDocument.Parse(yaml);

        // assert
        document.RootElement.ToJson().MatchInlineSnapshot(
            """
            {
              "planner": {
                "enableRequestGrouping": false,
                "maxPlanningTime": "00:00:05"
              },
              "sources": [
                {
                  "name": "accounts",
                  "url": "http://accounts/graphql"
                },
                {
                  "name": "reviews",
                  "headers": [
                    "Authorization",
                    "X-Request-Id"
                  ]
                }
              ],
              "tags": [
                "a",
                "b"
              ],
              "empty": [],
              "none": {},
              "missing": null
            }
            """);
    }

    [Fact]
    public void Parse_Should_ResolveScalarKinds_When_ScalarsArePlainOrQuoted()
    {
        // arrange
        const string yaml =
            """
            null: null
            true: true
            false: false
            integer: -12
            float: 1.5e3
            quotedTrue: "true"
            capitalTrue: True
            hex: 0x1F
            version: 1.2.3
            sentence: hello world, again
            """;

        // act
        var document = YamlDocument.Parse(yaml);

        // assert
        document.RootElement.ToJson().MatchInlineSnapshot(
            """
            {
              "null": null,
              "true": true,
              "false": false,
              "integer": -12,
              "float": 1.5e3,
              "quotedTrue": "true",
              "capitalTrue": "True",
              "hex": "0x1F",
              "version": "1.2.3",
              "sentence": "hello world, again"
            }
            """);
    }

    [Fact]
    public void Parse_Should_ReadNestedSequences_When_ItemsStartWithDashOrOwnLine()
    {
        // arrange
        const string yaml =
            """
            - - a
              - b
            -
              - c
            -
              key: value
            -
            """;

        // act
        var document = YamlDocument.Parse(yaml);

        // assert
        document.RootElement.ToJson().MatchInlineSnapshot(
            """
            [
              [
                "a",
                "b"
              ],
              [
                "c"
              ],
              {
                "key": "value"
              },
              null
            ]
            """);
    }

    [Fact]
    public void GetString_Should_DecodeEscapes_When_ScalarIsDoubleQuoted()
    {
        // arrange
        var document = YamlDocument.Parse(
            """
            "quoted \"key\"": "line\nbreak \\ tab\t \u00e9 # no comment"
            """);

        // act
        var property = document.RootElement.EnumerateObject().Single();

        // assert
        Assert.Equal("quoted \"key\"", property.Name);
        Assert.Equal("line\nbreak \\ tab\t é # no comment", property.Value.GetString());
    }

    [Fact]
    public void Parse_Should_ReturnNull_When_DocumentHasOnlyComments()
    {
        // act
        var document = YamlDocument.Parse("# nothing here\n\n");

        // assert
        Assert.Equal(YamlValueKind.Null, document.RootElement.ValueKind);
    }

    [Fact]
    public void Parse_Should_ReportPositions_When_ElementsAreRead()
    {
        // arrange
        var document = YamlDocument.Parse(
            """
            planner:
              enableRequestGrouping: false
            """);

        // act
        var property = document.RootElement.GetProperty("planner").EnumerateObject().Single();

        // assert
        Assert.Equal(2, property.Line);
        Assert.Equal(3, property.Column);
        Assert.Equal(2, property.Value.Line);
        Assert.Equal(26, property.Value.Column);
    }

    [Fact]
    public void GetInt32_Should_ReturnNumber_When_ValueIsInteger()
    {
        // arrange
        var document = YamlDocument.Parse("port: 8080");

        // act
        var port = document.RootElement.GetProperty("port").GetInt32();

        // assert
        Assert.Equal(8080, port);
    }

    [Fact]
    public void GetString_Should_Throw_When_ValueIsNumber()
    {
        // arrange
        var document = YamlDocument.Parse("port: 8080");
        var port = document.RootElement.GetProperty("port");

        // act
        var exception = Assert.Throws<InvalidOperationException>(port.GetString);

        // assert
        Assert.Equal(
            "The operation requires an element of kind 'String', but the element is of kind 'Number'.",
            exception.Message);
    }

    [Theory]
    [InlineData("key: &anchor value", "Anchors are not supported (line 1, column 6).")]
    [InlineData("key: *alias", "Aliases are not supported (line 1, column 6).")]
    [InlineData("key: !!str value", "Tags are not supported (line 1, column 6).")]
    [InlineData("key: |\n  text", "Block scalars are not supported (line 1, column 6).")]
    [InlineData("key: >\n  text", "Block scalars are not supported (line 1, column 6).")]
    [InlineData("key: 'value'", "Single-quoted strings are not supported. Use double quotes (line 1, column 6).")]
    [InlineData(
        "key: [a, b]",
        "Flow collections are not supported, except [] and {} for empty collections (line 1, column 6).")]
    [InlineData(
        "key: {a: b}",
        "Flow collections are not supported, except [] and {} for empty collections (line 1, column 6).")]
    [InlineData("---\nkey: value", "Document markers are not supported (line 1, column 1).")]
    [InlineData("? key\n: value", "Unexpected character '?' (line 1, column 1).")]
    [InlineData("key: value: other", "A value without quotes cannot contain ': ' or end with ':' (line 1, column 11).")]
    [InlineData("key: \"value", "The double-quoted string does not end on the same line (line 1, column 6).")]
    [InlineData("key: \"a\\qb\"", "The escape sequence is not valid (line 1, column 8).")]
    [InlineData("key: \"value\" rest", "Unexpected character 'r' (line 1, column 14).")]
    public void Parse_Should_Throw_When_SyntaxIsNotSupported(string yaml, string expectedMessage)
    {
        // act
        var exception = Assert.Throws<YamlException>(() => YamlDocument.Parse(yaml));

        // assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void Parse_Should_Throw_When_KeyIsDefinedTwice()
    {
        // act
        var exception = Assert.Throws<YamlException>(
            () => YamlDocument.Parse("a: 1\nb: 2\na: 3"));

        // assert
        Assert.Equal("The key 'a' is defined more than once (line 3, column 1).", exception.Message);
        Assert.Equal(3, exception.Line);
        Assert.Equal(1, exception.Column);
    }

    [Fact]
    public void Parse_Should_Throw_When_LineIsIndentedUnexpectedly()
    {
        // act
        var exception = Assert.Throws<YamlException>(
            () => YamlDocument.Parse("a:\n    b: 1\n  c: 2"));

        // assert
        Assert.Equal("The line has an unexpected indentation (line 3, column 3).", exception.Message);
    }

    [Fact]
    public void Parse_Should_Throw_When_ValueContinuesOnNextLine()
    {
        // act
        var exception = Assert.Throws<YamlException>(
            () => YamlDocument.Parse("a: first\n  second"));

        // assert
        Assert.Equal("The line has an unexpected indentation (line 2, column 3).", exception.Message);
    }

    [Fact]
    public void Parse_Should_Throw_When_IndentationUsesTabs()
    {
        // act
        var exception = Assert.Throws<YamlException>(
            () => YamlDocument.Parse("a:\n\tb: 1"));

        // assert
        Assert.Equal("Tabs cannot be used for indentation (line 2, column 1).", exception.Message);
    }

    [Fact]
    public void Parse_Should_Throw_When_MappingContainsScalarLine()
    {
        // act
        var exception = Assert.Throws<YamlException>(
            () => YamlDocument.Parse("a: 1\njust text"));

        // assert
        Assert.Equal("Expected a key followed by ':' (line 2, column 1).", exception.Message);
    }
}
