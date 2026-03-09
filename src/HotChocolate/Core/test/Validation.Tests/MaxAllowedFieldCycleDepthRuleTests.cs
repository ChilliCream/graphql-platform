using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class MaxAllowedFieldCycleDepthRuleTests()
    : DocumentValidatorVisitorTestBase(b => b.AddMaxAllowedFieldCycleDepthRule())
{
    [Fact]
    public void Max_3_Cycles_Allowed_Success()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            {
                human {
                    relatives {
                        relatives {
                            relatives {
                                name
                            }
                        }
                    }
                }
            }
            """);

        var context = ValidationUtils.CreateContext(document);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Empty(context.Errors);
        Assert.False(context.FatalErrorDetected);
        Assert.False(context.UnexpectedErrorsDetected);
    }

    [Fact]
    public void Max_3_Cycles_Allowed_Fail()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            {
                human {
                    relatives {
                        relatives {
                            relatives {
                                relatives {
                                    name
                                }
                            }
                        }
                    }
                }
            }
            """);

        var context = ValidationUtils.CreateContext(document);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Equal(
            "Maximum allowed coordinate cycle depth was exceeded.",
            Assert.Single(context.Errors).Message);
        Assert.True(context.FatalErrorDetected);
        Assert.False(context.UnexpectedErrorsDetected);
    }

    [Fact]
    public void Max_2_Relative_Field_Allowed_Success()
    {
        // arrange
        var validator =
            DocumentValidatorBuilder.New()
                .AddMaxAllowedFieldCycleDepthRule(
                    null,
                    [(new SchemaCoordinate("Human", "relatives"), 2)])
                .ModifyOptions(o => o.MaxAllowedErrors = int.MaxValue)
                .Build();

        var rule = validator.Rules[0];

        var document = Utf8GraphQLParser.Parse(
            """
            {
                human {
                    relatives {
                        relatives {
                            name
                        }
                    }
                }
            }
            """);

        var context = ValidationUtils.CreateContext(document);

        // act
        rule.Validate(context, document);

        // assert
        Assert.Empty(context.Errors);
        Assert.False(context.FatalErrorDetected);
        Assert.False(context.UnexpectedErrorsDetected);
    }

    [Fact]
    public void Max_1_Relative_Field_Allowed_Fail()
    {
        // arrange
        var validator =
            DocumentValidatorBuilder.New()
                .AddMaxAllowedFieldCycleDepthRule(
                    null,
                    [(new SchemaCoordinate("Human", "relatives"), 1)])
                .ModifyOptions(o => o.MaxAllowedErrors = int.MaxValue)
                .Build();

        var rule = validator.Rules[0];

        var document = Utf8GraphQLParser.Parse(
            """
            {
                human {
                    relatives {
                        relatives {
                            name
                        }
                    }
                }
            }
            """);

        var context = ValidationUtils.CreateContext(document);

        // act
        rule.Validate(context, document);

        // assert
        Assert.Equal(
            "Maximum allowed coordinate cycle depth was exceeded.",
            Assert.Single(context.Errors).Message);
        Assert.True(context.FatalErrorDetected);
        Assert.False(context.UnexpectedErrorsDetected);
    }
}
