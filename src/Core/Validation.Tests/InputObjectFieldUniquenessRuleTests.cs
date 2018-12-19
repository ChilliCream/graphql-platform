
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class InputObjectFieldUniquenessRuleTests
        : ValidationTestBase
    {
        public InputObjectFieldUniquenessRuleTests()
            : base(new InputObjectFieldUniquenessRule())
        {
        }

        [Fact]
        public void NoFieldAmbiguity()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog(complex: { name: ""A"", owner: ""B"" })
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void NameFieldIsAmbiguous()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog(complex: { name: ""A"", name: ""B"" })
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "Field `name` is ambiguous.",
                    t.Message));
        }
    }
}
