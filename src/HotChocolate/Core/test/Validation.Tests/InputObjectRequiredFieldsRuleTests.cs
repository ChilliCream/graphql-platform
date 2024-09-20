using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class InputObjectRequiredFieldsRuleTests
    : DocumentValidatorVisitorTestBase
{
    public InputObjectRequiredFieldsRuleTests()
        : base(builder => builder.AddValueRules())
    {
    }

    [Fact]
    public void RequiredFieldsHaveValidValue()
    {
        ExpectValid(@"
                {
                    findDog2(complex: { name: ""Foo"" })
                }
            ");
    }

    [Fact]
    public void NestedRequiredFieldsHaveValidValue()
    {
        ExpectValid(@"
                {
                    findDog2(complex: { name: ""Foo"" child: { name: ""123"" } })
                }
            ");
    }

    [Fact]
    public void RequiredFieldIsNull()
    {
        ExpectErrors(@"
                {
                    findDog2(complex: { name: null })
                }
            ",
            t => Assert.Equal(
                "`name` is a required field and cannot be null.",
                t.Message));
    }

    [Fact]
    public void RequiredFieldIsNotSet()
    {
        // arrange
        ExpectErrors(@"
                {
                    findDog2(complex: { })
                }
            ",
            t => Assert.Equal(
                "`name` is a required field and cannot be null.",
                t.Message));
    }

    [Fact]
    public void NestedRequiredFieldIsNotSet()
    {
        // arrange
        ExpectErrors(@"
                {
                    findDog2(complex: { name: ""foo"" child: { owner: ""bar"" } })
                }
            ",
            t => Assert.Equal(
                "`name` is a required field and cannot be null.",
                t.Message));
    }

    [Fact]
    public void BadNullToNonNullField()
    {
        ExpectErrors(@"
                {
                    arguments {
                        complexArgField(complexArg: {
                            requiredField: true,
                            nonNullField: null,
                        })
                    }
                }
            ");
    }
}
