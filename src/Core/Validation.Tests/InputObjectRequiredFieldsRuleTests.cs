using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class InputObjectRequiredFieldsRuleTests
        : ValidationTestBase
    {
        public InputObjectRequiredFieldsRuleTests()
            : base(new InputObjectRequiredFieldsRule())
        {
        }

        [Fact]
        public void RequiredFieldsHaveValidValue()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog2(complex: { name: ""Foo"" })
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void NestedRequiredFieldsHaveValidValue()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog2(complex: { name: ""Foo"" child: { name: ""123"" } })
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void RequiredFieldIsNull()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog2(complex: { name: null })
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "`name` is a required field and cannot be null.",
                    t.Message));
        }

        [Fact]
        public void RequiredFieldIsNotSet()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog2(complex: { })
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "`name` is a required field and cannot be null.",
                    t.Message));
        }

        [Fact]
        public void NestedRequiredFieldIsNotSet()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog2(complex: { name: ""foo"" child: { owner: ""bar"" } })
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "`name` is a required field and cannot be null.",
                    t.Message));
        }
    }
}
