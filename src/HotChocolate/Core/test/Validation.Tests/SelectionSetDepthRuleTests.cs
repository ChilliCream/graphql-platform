using System.Text;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class SelectionSetDepthRuleTests
{
    [Fact]
    public void Validate_Should_AcceptOperation_When_ChainedFragmentsAreWithinLimit()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse("{ ...F0 } " + CreateFragmentChain(fragments: 3));

        // act
        var result = Validate(document, maxAllowedSelectionSetDepth: 7);

        // assert
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Validate_Should_RejectOperation_When_ChainedFragmentsExceedLimit()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse("{ ...F0 } " + CreateFragmentChain(fragments: 3));

        // act
        var result = Validate(document, maxAllowedSelectionSetDepth: 6);

        // assert
        var error = Assert.Single(result.Errors);
        Assert.Equal("The document exceeds the maximum allowed selection set depth of 6.", error.Message);
        Assert.Equal(ErrorCodes.Validation.MaxSelectionSetDepthOverflow, error.Code);
    }

    [Fact]
    public void Validate_Should_RejectOperation_When_ChainedFragmentsAreAlsoSpreadDirectly()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            "{ ...F2 ...F1 ...F0 } " + CreateFragmentChain(fragments: 3));

        // act
        var result = Validate(document, maxAllowedSelectionSetDepth: 6);

        // assert
        Assert.Equal(
            ErrorCodes.Validation.MaxSelectionSetDepthOverflow,
            Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Validate_Should_RejectOperation_When_SecondOperationExceedsLimit()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            "query A { __typename } query B { ...F0 } " + CreateFragmentChain(fragments: 3));

        // act
        var result = Validate(document, maxAllowedSelectionSetDepth: 6);

        // assert
        Assert.Equal(
            ErrorCodes.Validation.MaxSelectionSetDepthOverflow,
            Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Validate_Should_CountFragmentDepth_When_FragmentIsSpreadInNestedField()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            {
                human {
                    ...A
                }
            }

            fragment A on Human {
                relatives {
                    name
                }
            }
            """);

        // act
        var valid = Validate(document, maxAllowedSelectionSetDepth: 4);
        var invalid = Validate(document, maxAllowedSelectionSetDepth: 3);

        // assert
        Assert.False(valid.HasErrors);
        var error = Assert.Single(invalid.Errors);
        Assert.Equal("The document exceeds the maximum allowed selection set depth of 3.", error.Message);
        Assert.Equal(Path.Root.Append("human").Append("relatives"), error.Path);
    }

    [Fact]
    public void Validate_Should_AcceptOperation_When_FragmentIsSpreadRepeatedly()
    {
        // arrange
        var sourceText = new StringBuilder("{ ...F0 }");

        for (var i = 0; i < 64; i++)
        {
            sourceText.Append($" fragment F{i} on Query {{ ...F{i + 1} ...F{i + 1} }}");
        }

        sourceText.Append(" fragment F64 on Query { __typename }");

        var document = Utf8GraphQLParser.Parse(sourceText.ToString());

        // act
        var result = Validate(document);

        // assert
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Validate_Should_ReportOnlyFragmentCycle_When_FragmentSpreadsFormCycle()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            {
                unknownField
                dog {
                    ...A
                }
            }

            fragment A on Dog {
                ...B
            }

            fragment B on Dog {
                ...A
            }
            """);

        // act
        var result = Validate(document);

        // assert
        var error = Assert.Single(result.Errors);
        Assert.StartsWith("The graph of fragment spreads must not form any cycles", error.Message);
        Assert.Equal(Path.Root.Append("dog"), error.Path);
    }

    [Fact]
    public void Validate_Should_ReportUnknownFragment_When_SpreadFragmentDoesNotExist()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse("{ ...Unknown }");

        // act
        var result = Validate(document);

        // assert
        Assert.Equal(
            "The specified fragment `Unknown` does not exist.",
            Assert.Single(result.Errors).Message);
    }

    private static DocumentValidatorResult Validate(
        DocumentNode document,
        int? maxAllowedSelectionSetDepth = null)
    {
        var builder = DocumentValidatorBuilder.New().AddDefaultRules();

        if (maxAllowedSelectionSetDepth.HasValue)
        {
            builder.ModifyOptions(o => o.MaxAllowedSelectionSetDepth = maxAllowedSelectionSetDepth.Value);
        }

        return builder.Build().Validate(ValidationUtils.CreateSchema(), document);
    }

    private static string CreateFragmentChain(int fragments)
    {
        var sourceText = new StringBuilder();

        for (var i = 0; i < fragments; i++)
        {
            sourceText.Append($"fragment F{i} on Query {{ ... {{ ");
            sourceText.Append(i + 1 < fragments ? $"...F{i + 1}" : "__typename");
            sourceText.Append(" } } ");
        }

        return sourceText.ToString();
    }
}
