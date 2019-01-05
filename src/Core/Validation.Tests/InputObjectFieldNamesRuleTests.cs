using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class InputObjectFieldNamesRuleTests
        : ValidationTestBase
    {
        public InputObjectFieldNamesRuleTests()
            : base(new InputObjectFieldNamesRule())
        {
        }

        [Fact]
        public void AllInputObjectFieldsExist()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog(complex: { name: ""Fido"" })
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }


        [Fact]
        public void InvalidInputObjectFieldsExist()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog(complex: { favoriteCookieFlavor: ""Bacon"" })
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified input object field " +
                    "`favoriteCookieFlavor` does not exist.",
                    t.Message));
        }

        [Fact]
        public void InvalidNestedInputObjectFieldsExist()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog(complex: { child: { favoriteCookieFlavor: ""Bacon"" } })
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified input object field " +
                    "`favoriteCookieFlavor` does not exist.",
                    t.Message));
        }
    }
}
