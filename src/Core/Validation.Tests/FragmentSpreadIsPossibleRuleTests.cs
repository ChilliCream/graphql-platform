using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentSpreadIsPossibleRuleTests
        : ValidationTestBase
    {
        public FragmentSpreadIsPossibleRuleTests()
            : base(new FragmentSpreadIsPossibleRule())
        {
        }

        [Fact]
        public void FragmentDoesNotMatchType()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...fragmentDoesNotMatchType
                    }
                }

                fragment fragmentDoesNotMatchType on Human {
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "The parent type does not match the type condition on " +
                    "the fragment `fragmentDoesNotMatchType`."));
        }

        [Fact]
        public void InterfaceTypeDoesMatch()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...interfaceTypeDoesMatch
                    }
                }

                fragment interfaceTypeDoesMatch on Pet {
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void UnionTypeDoesMatch()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...unionTypeDoesMatch
                    }
                }

                fragment unionTypeDoesMatch on CatOrDog {
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

         [Fact]
        public void ObjectTypeDoesMatch()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...objectTypeDoesMatch
                    }
                }

                fragment objectTypeDoesMatch on Dog {
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }
    }
}
