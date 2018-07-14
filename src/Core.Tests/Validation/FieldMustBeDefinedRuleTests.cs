using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class FieldMustBeDefinedRuleTests
    {
        [Fact]
        public void FieldIsNotDefinedOnTypeInFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment fieldNotDefined on Dog {
                    meowVolume
                }

                fragment aliasedLyingFieldTargetNotDefined on Dog {
                    barkVolume: kawVolume
                }
            ");

            // act
            var validator = new FieldMustBeDefinedRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The field `meowVolume` does not exist " +
                    "on the type `Dog`.", t.Message),
                t => Assert.Equal(
                    "The field `kawVolume` does not exist " +
                    "on the type `Dog`.", t.Message));
        }

        [Fact]
        public void InterfaceFieldSelectionOnPet()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment interfaceFieldSelection on Pet {
                    name
                }
            ");

            // act
            var validator = new FieldMustBeDefinedRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void DefinedOnImplementorsButNotInterfaceOnPet()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment definedOnImplementorsButNotInterface on Pet {
                    nickname
                }
            ");

            // act
            var validator = new FieldMustBeDefinedRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The field `nickname` does not exist " +
                    "on the type `Pet`.", t.Message));
        }

        [Fact]
        public void InDirectFieldSelectionOnUnion()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
            fragment inDirectFieldSelectionOnUnion on CatOrDog {
                __typename
                ... on Pet {
                    name
                }
                ... on Dog {
                    barkVolume
                }
            }
            ");

            // act
            var validator = new FieldMustBeDefinedRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }


        [Fact]
        public void DirectFieldSelectionOnUnion()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment directFieldSelectionOnUnion on CatOrDog {
                    name
                    barkVolume
                }
            ");

            // act
            var validator = new FieldMustBeDefinedRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "A union type cannot declare a field directly. " +
                    "Use inline fragments or fragments instead", t.Message));
        }
    }
}
