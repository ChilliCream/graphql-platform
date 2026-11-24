using System.Buffers;
using System.Text;

namespace HotChocolate.Text.Yaml;

public class YamlWriterTests
{
    [Fact]
    public void Write_Should_WriteBlockStyle_When_ObjectsAndArraysAreNested()
    {
        // act
        var yaml = Write(writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("planner");
            writer.WriteStartObject();
            writer.WritePropertyName("enableRequestGrouping");
            writer.WriteBooleanValue(false);
            writer.WriteEndObject();
            writer.WritePropertyName("nodes");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WritePropertyName("id");
            writer.WriteNumberValue(1);
            writer.WritePropertyName("dependencies");
            writer.WriteStartArray();
            writer.WriteNumberValue(2);
            writer.WriteNumberValue(3);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteStartArray();
            writer.WriteStringValue("nested");
            writer.WriteEndArray();
            writer.WriteNullValue();
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        // assert
        yaml.MatchInlineSnapshot(
            """
            planner:
              enableRequestGrouping: false
            nodes:
              - id: 1
                dependencies:
                  - 2
                  - 3
              -
                - nested
              - null

            """);
    }

    [Fact]
    public void Write_Should_WriteEmptyCollections_When_ContainersHaveNoItems()
    {
        // act
        var yaml = Write(writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("object");
            writer.WriteStartObject();
            writer.WriteEndObject();
            writer.WritePropertyName("array");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteEndObject();
            writer.WriteStartArray();
            writer.WriteEndArray();
            writer.WriteEndArray();
            writer.WritePropertyName("flow");
            writer.WriteStartFlowArray();
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        // assert
        yaml.MatchInlineSnapshot(
            """
            object: {}
            array:
              - {}
              - []
            flow: []

            """);
    }

    [Fact]
    public void Write_Should_WriteItemsOnOneLine_When_ArrayIsFlowArray()
    {
        // act
        var yaml = Write(writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("scopes");
            writer.WriteStartArray();
            writer.WriteStartFlowArray();
            writer.WriteStringValue("read");
            writer.WriteStringValue("write all");
            writer.WriteEndArray();
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        // assert
        yaml.MatchInlineSnapshot(
            """
            scopes:
              - [read, "write all"]

            """);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("$.products<Product>.name", "$.products<Product>.name")]
    [InlineData("ID!", "ID!")]
    [InlineData("http://localhost:5000/graphql", "http://localhost:5000/graphql")]
    [InlineData("08674244ab", "08674244ab")]
    [InlineData("", "\"\"")]
    [InlineData("true", "\"true\"")]
    [InlineData("Yes", "\"Yes\"")]
    [InlineData("12", "\"12\"")]
    [InlineData("1e5", "\"1e5\"")]
    [InlineData("0b101", "\"0b101\"")]
    [InlineData("key:", "\"key:\"")]
    [InlineData("two words", "\"two words\"")]
    [InlineData("[ID!]!", "\"[ID!]!\"")]
    [InlineData("say \"hi\"\n\\", "\"say \\\"hi\\\"\\n\\\\\"")]
    public void WriteStringValue_Should_QuoteString_When_PlainWouldReadBackDifferently(
        string value,
        string expected)
    {
        // act
        var yaml = Write(writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteStringValue(value);
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal($"value: {expected}\n", yaml);
    }

    [Fact]
    public void WriteLiteralStringValue_Should_ChooseChomping_When_ValueEndsWithLineBreaks()
    {
        // act
        var yaml = Write(writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("strip");
            writer.WriteLiteralStringValue("a\n\nb");
            writer.WritePropertyName("clip");
            writer.WriteLiteralStringValue("a\n");
            writer.WritePropertyName("keep");
            writer.WriteLiteralStringValue("a\n\n");
            writer.WritePropertyName("indented");
            writer.WriteLiteralStringValue("  a");
            writer.WriteEndObject();
        });

        // assert
        yaml.MatchInlineSnapshot(
            """
            strip: |-
              a

              b
            clip: |
              a
            keep: |+
              a

            indented: "  a"

            """);
    }

    [Fact]
    public void WriteFoldedStringValue_Should_WriteLiteral_When_FoldingChangesValue()
    {
        // act
        var yaml = Write(writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("folded");
            writer.WriteFoldedStringValue("{\n  id\n}");
            writer.WritePropertyName("literal");
            writer.WriteFoldedStringValue("a\nb");
            writer.WriteEndObject();
        });

        // assert
        yaml.MatchInlineSnapshot(
            """
            folded: >-
              {
                id
              }
            literal: |-
              a
              b

            """);
    }

    [Fact]
    public void Write_Should_ReadBackSameValues_When_OutputIsParsed()
    {
        // arrange
        var yaml = Write(writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("text");
            writer.WriteStringValue("needs: quotes # here");
            writer.WritePropertyName("keyword");
            writer.WriteStringValue("null");
            writer.WritePropertyName("number");
            writer.WriteNumberValue(1.5);
            writer.WritePropertyName("items");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WritePropertyName("two words");
            writer.WriteStringValue("é");
            writer.WriteEndObject();
            writer.WriteStartArray();
            writer.WriteEndArray();
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        // act
        var document = YamlDocument.Parse(yaml);

        // assert
        document.RootElement.ToJson().MatchInlineSnapshot(
            """
            {
              "text": "needs: quotes # here",
              "keyword": "null",
              "number": 1.5,
              "items": [
                {
                  "two words": "é"
                },
                []
              ]
            }
            """);
    }

    private static string Write(Action<YamlWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        write(new YamlWriter(buffer));
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
