using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class IntrospectionDepthRuleTests()
    : DocumentValidatorVisitorTestBase(b => b.AddIntrospectionDepthRule())
{
    [Fact]
    public void Introspection_With_Cycles_Will_Fail()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(FileResource.Open("introspection_with_cycle.graphql"));
        var context = ValidationUtils.CreateContext(document);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Equal(
            "Maximum allowed introspection depth exceeded.",
            Assert.Single(context.Errors).Message);
    }

    [Fact]
    public void Introspection_Without_Cycles()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(FileResource.Open("introspection_without_cycle.graphql"));
        var context = ValidationUtils.CreateContext(document);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Empty(context.Errors);
    }
}
