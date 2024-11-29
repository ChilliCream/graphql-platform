using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class InputObjectFieldNamesRuleTests
    : DocumentValidatorVisitorTestBase
{
    public InputObjectFieldNamesRuleTests()
        : base(builder => builder.AddValueRules())
    {
    }

    [Fact]
    public void AllInputObjectFieldsExist()
    {
        ExpectValid(@"
                {
                    findDog(complex: { name: ""Fido"" })
                }
            ");
    }

    [Fact]
    public void InvalidInputObjectFieldsExist()
    {
        ExpectErrors(@"
                {
                    findDog(complex: { favoriteCookieFlavor: ""Bacon"" })
                }
            ",
            t => Assert.Equal(
                "The specified input object field " +
                "`favoriteCookieFlavor` does not exist.",
                t.Message));
    }

    [Fact]
    public void InvalidNestedInputObjectFieldsExist()
    {
        // arrange
        ExpectErrors(@"
                {
                    findDog(complex: { child: { favoriteCookieFlavor: ""Bacon"" } })
                }
            ",
            t => Assert.Equal(
                "The specified input object field " +
                "`favoriteCookieFlavor` does not exist.",
                t.Message));
    }
}
