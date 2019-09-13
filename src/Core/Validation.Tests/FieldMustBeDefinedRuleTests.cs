using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class FieldMustBeDefinedRuleTests
        : ValidationTestBase
    {
        public FieldMustBeDefinedRuleTests()
            : base(new FieldMustBeDefinedRule())
        {
        }

        [Fact]
        public void FieldIsNotDefinedOnTypeInFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                fragment fieldNotDefined on Dog {
                    meowVolume
                }

                fragment aliasedLyingFieldTargetNotDefined on Dog {
                    barkVolume: kawVolume
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

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
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                fragment interfaceFieldSelection on Pet {
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void DefinedOnImplementorsButNotInterfaceOnPet()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                fragment definedOnImplementorsButNotInterface on Pet {
                    nickname
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

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
            DocumentNode query = Utf8GraphQLParser.Parse(@"
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
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }


        [Fact]
        public void DirectFieldSelectionOnUnion()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                fragment directFieldSelectionOnUnion on CatOrDog {
                    name
                    barkVolume
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "A union type cannot declare a field directly. " +
                    "Use inline fragments or fragments instead", t.Message));
        }


        [Fact]
        public void IntrospectionFieldsOnInterface()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                fragment interfaceFieldSelection on Pet {
                    __typename
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void IntrospectionFieldsOnUnion()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                fragment interfaceFieldSelection on CatOrDog {
                    __typename
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void IntrospectionFieldsOnObject()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                fragment interfaceFieldSelection on Cat {
                    __typename
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }
    }
}
