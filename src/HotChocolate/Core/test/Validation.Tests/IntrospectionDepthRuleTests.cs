using HotChocolate.Language;
using HotChocolate.Validation.Options;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class IntrospectionDepthRuleTests()
    : DocumentValidatorVisitorTestBase(b => b.AddIntrospectionDepthRule())
{
    [Fact] public void Introspection_With_Cycles_Will_Fail()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();

        var query = Utf8GraphQLParser.Parse(FileResource.Open("introspection_with_cycle.graphql"));
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Equal(
            "Maximum allowed introspection depth exceeded.",
            Assert.Single(context.Errors).Message);
    }

    [Fact]
    public void Introspection_Without_Cycles()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();

        var query = Utf8GraphQLParser.Parse(FileResource.Open("introspection_without_cycle.graphql"));
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Empty(context.Errors);
    }
}

public class MaxAllowedFieldCycleDepthRuleTests()
    : DocumentValidatorVisitorTestBase(b => b.AddMaxAllowedFieldCycleDepthRule())
{
    [Fact]
    public void Max_3_Cycles_Allowed_Success()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();

        var query = Utf8GraphQLParser.Parse(
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
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Empty(context.Errors);
        Assert.False(context.FatalErrorDetected);
        Assert.False(context.UnexpectedErrorsDetected);
    }

    [Fact]
    public void Max_3_Cycles_Allowed_Fail()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();

        var query = Utf8GraphQLParser.Parse(
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
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

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
        var serviceCollection = new ServiceCollection();

        var builder = serviceCollection
            .AddValidation()
            .ConfigureValidation(c => c.RulesModifiers.Add((_, r) => r.Rules.Clear()))
            .ModifyValidationOptions(o => o.MaxAllowedErrors = int.MaxValue);
        builder.AddMaxAllowedFieldCycleDepthRule(
            null,
            [(new SchemaCoordinate("Human", "relatives"), 2)]);

        IServiceProvider services = serviceCollection.BuildServiceProvider();

        var rule = services
            .GetRequiredService<IValidationConfiguration>()
            .GetRules(Schema.DefaultName).First();

        IDocumentValidatorContext context = ValidationUtils.CreateContext();

        var query = Utf8GraphQLParser.Parse(
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
        context.Prepare(query);

        // act
        rule.Validate(context, query);

        // assert
        Assert.Empty(context.Errors);
        Assert.False(context.FatalErrorDetected);
        Assert.False(context.UnexpectedErrorsDetected);
    }

    [Fact]
    public void Max_1_Relative_Field_Allowed_Fail()
    {
        // arrange
        var serviceCollection = new ServiceCollection();

        var builder = serviceCollection
            .AddValidation()
            .ConfigureValidation(c => c.RulesModifiers.Add((_, r) => r.Rules.Clear()))
            .ModifyValidationOptions(o => o.MaxAllowedErrors = int.MaxValue);
        builder.AddMaxAllowedFieldCycleDepthRule(
            null,
            [(new SchemaCoordinate("Human", "relatives"), 1)]);

        IServiceProvider services = serviceCollection.BuildServiceProvider();

        var rule = services
            .GetRequiredService<IValidationConfiguration>()
            .GetRules(Schema.DefaultName).First();

        IDocumentValidatorContext context = ValidationUtils.CreateContext();

        var query = Utf8GraphQLParser.Parse(
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
        context.Prepare(query);

        // act
        rule.Validate(context, query);

        // assert
        Assert.Equal(
            "Maximum allowed coordinate cycle depth was exceeded.",
            Assert.Single(context.Errors).Message);
        Assert.True(context.FatalErrorDetected);
        Assert.False(context.UnexpectedErrorsDetected);
    }
}
