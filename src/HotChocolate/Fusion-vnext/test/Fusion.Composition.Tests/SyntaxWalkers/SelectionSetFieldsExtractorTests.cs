using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.SyntaxWalkers;

public sealed class SelectionSetFieldsExtractorTests
{
    [Theory]
#pragma warning disable xUnit1044
    [MemberData(nameof(ExamplesData))]
#pragma warning restore xUnit1044
    public void Examples(string selectionSetText, (string, string, string)[] expected)
    {
        // arrange
        var selectionSet = ParseSelectionSet(selectionSetText);
        var extractor = new SelectionSetFieldsExtractor(s_schema);

        // act
        var fields = extractor.ExtractFields(selectionSet, s_schema.QueryType!);

        // assert
        Assert.Equal(expected.Length, fields.Length);

        for (var i = 0; i < fields.Length; i++)
        {
            var (field, type, _) = fields[i];

            Assert.Equal(expected[i].Item1, field.Name);
            Assert.Equal(expected[i].Item2, field.Type.AsTypeDefinition().Name);
            Assert.Equal(expected[i].Item3, type.Name);
        }
    }

    public static TheoryData<string, (string, string, string)[]> ExamplesData()
    {
        return new TheoryData<string, (string, string, string)[]>
        {
            // Nested.
            {
                "{ a { b { c d } } e { f { g h } } } }",
                [
                    // FieldName, FieldTypeName, DeclaringTypeName.
                    ("a", "A", "Query"),
                    ("b", "B", "A"),
                    ("c", "Int", "B"),
                    ("d", "Int", "B"),
                    ("e", "E", "Query"),
                    ("f", "F", "E"),
                    ("g", "Int", "F"),
                    ("h", "Int", "F")
                ]
            },
            // Inline fragments.
            {
                "{ ... on Query { a { b { c d } } } ... on Query { e { f { g h } } } }",
                [
                    // FieldName, FieldTypeName, DeclaringTypeName.
                    ("a", "A", "Query"),
                    ("b", "B", "A"),
                    ("c", "Int", "B"),
                    ("d", "Int", "B"),
                    ("e", "E", "Query"),
                    ("f", "F", "E"),
                    ("g", "Int", "F"),
                    ("h", "Int", "F")
                ]
            }
        };
    }

    private static readonly MutableSchemaDefinition s_schema = SchemaParser.Parse(
        """
        type Query {
            a: A!
            e: E!
        }

        type A {
            b: B!
        }

        type B {
            c: Int!
            d: Int!
        }

        type E {
            f: F!
        }

        type F {
            g: Int!
            h: Int!
        }
        """);
}
