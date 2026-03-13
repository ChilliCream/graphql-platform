using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Validators;

public sealed class FieldInSelectionSetValidatorTests
{
    [Theory]
    [MemberData(nameof(ExamplesData))]
    public void Examples(
        string selectionSetText,
        string fieldName,
        string fieldDeclaringTypeName,
        bool expectedResult)
    {
        // arrange
        var selectionSet = ParseSelectionSet(selectionSetText);
        var fieldDeclaringType =
            s_schema.Types
                .OfType<MutableObjectTypeDefinition>()
                .FirstOrDefault(t => t.Name == fieldDeclaringTypeName);

        // act
        bool result;

        if (fieldDeclaringType is null)
        {
            result = false;
        }
        else if (!fieldDeclaringType.Fields.TryGetField(fieldName, out var field))
        {
            result = false;
        }
        else
        {
            result =
                new FieldInSelectionSetValidator(s_schema).Validate(
                    selectionSet,
                    s_schema.QueryType!,
                    field,
                    fieldDeclaringType);
        }

        // assert
        Assert.Equal(expectedResult, result);
    }

    public static TheoryData<string, string, string, bool> ExamplesData()
    {
        // SelectionSetText, FieldName, FieldDeclaringTypeName, ExpectedResult.
        return new TheoryData<string, string, string, bool>
        {
            // Simple (matching type).
            {
                "{ a }",
                "a",
                "Query",
                true
            },
            // Simple (non-matching type).
            {
                "{ a }",
                "a",
                "A",
                false
            },
            // Simple (missing field).
            {
                "{ a }",
                "x",
                "Query",
                false
            },
            // Nested (matching type).
            {
                "{ a { b { c } } }",
                "c",
                "B",
                true
            },
            // Nested (non-matching type).
            {
                "{ a { b { c } } }",
                "c",
                "A",
                false
            },
            // Nested (missing field).
            {
                "{ a { b { c } } }",
                "x",
                "B",
                false
            },
            // Inline fragment (matching type).
            {
                "{ a { b { c { ... on D { d } } } } }",
                "d",
                "D",
                true
            },
            // Inline fragment (non-matching type).
            {
                "{ a { b { c { ... on D { d } } } } }",
                "d",
                "A",
                false
            },
            // Inline fragment (missing field).
            {
                "{ a { b { c { ... on D { d } } } } }",
                "x",
                "D",
                false
            }
        };
    }

    private static readonly MutableSchemaDefinition s_schema = SchemaParser.Parse(
        """
        type Query {
            a: A!
        }

        type A {
            b: B!
        }

        type B {
            c: C!
        }

        union C = D | E

        type D {
            d: Int!
        }

        type E {
            e: Int!
        }
        """);
}
