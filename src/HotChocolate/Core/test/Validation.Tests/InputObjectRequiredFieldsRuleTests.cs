using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Validation
{
    public class InputObjectRequiredFieldsRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public InputObjectRequiredFieldsRuleTests()
            : base(services => services.AddInputObjectRules())
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
                "The specified value type of field `name` " +
                    "does not match the field type.",
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
    }
}
