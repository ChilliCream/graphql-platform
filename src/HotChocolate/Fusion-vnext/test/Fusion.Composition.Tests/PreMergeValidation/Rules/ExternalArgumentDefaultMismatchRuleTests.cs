using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class ExternalArgumentDefaultMismatchRuleTests : CompositionTestBase
{
    private sealed class TestContext<TRule> where TRule : class, new()
    {
        public CompositionContext Context { get; }

        public PreMergeValidator Sut { get; } = new([new TRule()]);

        public TestContext(string[] sdl)
        {
            Context = new CompositionContext(
                [
                    .. sdl.Select((s, i) =>
                    {
                        var schemaDefinition = SchemaParser.Parse(s);
                        schemaDefinition.Name = ((char)('A' + i)).ToString();

                        return schemaDefinition;
                    })
                ],
                new CompositionLog());
        }

        public void VerifyValid()
        {
            // act
            var result = Sut.Validate(Context);

            // assert
            Assert.True(result.IsSuccess);
            Assert.True(Context.Log.IsEmpty);
        }

        public void VerifyInvalid(string[] errorMessages)
        {
            var result = Sut.Validate(Context);

            // assert
            Assert.True(result.IsFailure);
            Assert.Collection(Context.Log,
                errorMessages.Select(m => new Action<LogEntry>(entry =>
                {
                    Assert.Equal(m, entry.Message);
                    Assert.Equal("EXTERNAL_ARGUMENT_DEFAULT_MISMATCH", entry.Code);
                    Assert.Equal(LogSeverity.Error, entry.Severity);
                })).ToArray());
        }
    }

    [Theory]
    // Here, the `name` field on Product is defined in one source schema and marked as
    // @external in another. The argument `language` has the same default value in both
    // source schemas, satisfying the rule.
    [ValidInlineData(Sdl =
    [
        """
        type Product {
            name(language: String = "en"): String
        }
        """,
        """
        type Product {
            name(language: String = "en"): String @external
        }
        """
    ])]
    // Here, the `name` field on Product is defined with multiple arguments. Both arguments
    // have the same default value in the source schemas, satisfying the rule.
    [ValidInlineData(Sdl =
    [
        """
        type Product {
            name(language: String = "en", localization: String = "sr"): String
        }
        """,
        """
        type Product {
            name(localization: String = "sr", language: String = "en"): String @external
        }
        """,
        """
        type Product {
            name(language: String = "en", localization: String = "sr"): String @external
        }
        """
    ])]
    public void Examples_Valid(string[] sdl)
    {
        var ctx = new TestContext<ExternalArgumentDefaultMismatchRule>(sdl);
        ctx.VerifyValid();
    }

    [Theory]
    [InvalidInlineData(
        Sdl =
        [
            """
            type Product {
                name(language: String = "en"): String
            }
            """,
            """
            type Product {
                name(language: String = "de"): String @external
            }
            """
        ],
        ErrorMessages =
        [
            "The argument with schema coordinate 'Product.name(language:)' has " +
            "inconsistent default values."
        ])]
    public void Examples_Invalid(string[] sdl, string[] errorMessages)
    {
        // arrange
        var ctx = new TestContext<ExternalArgumentDefaultMismatchRule>(sdl);
        ctx.VerifyInvalid(errorMessages);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid2((string[] sdl, string[] errorMessages) _)
    {
        // arrange
        var (sdl, errorMessages) = _;
        var ctx = new TestContext<ExternalArgumentDefaultMismatchRule>(sdl);
        ctx.VerifyInvalid(errorMessages);
    }

    public static IEnumerable<TheoryDataRow<(string[] Sdl, string[] ErrorMessages)>> InvalidExamplesData()
    {
        // Here, the `name` field on Product is defined in one source schema and marked as
        // @external in another. The argument `language` has different default values in the
        // two source schemas, violating the rule.
        yield return (
            Sdl:
            [
                """
                type Product {
                    name(language: String = "en"): String
                }
                """,
                """
                type Product {
                    name(language: String = "de"): String @external
                }
                """
            ],
            ErrorMessages:
            [
                "The argument with schema coordinate 'Product.name(language:)' has " +
                "inconsistent default values."
            ]);
    }
}
