using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentSpreadTypeExistenceRuleTests
        : ValidationTestBase
    {
        public FragmentSpreadTypeExistenceRuleTests()
            : base(new FragmentSpreadTypeExistenceRule())
        {
        }

        [Fact]
        public void CorrectTypeOnFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...correctType
                    }
                }

                fragment correctType on Dog {
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void CorrectTypeOnInlineFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...inlineFragment
                    }
                }

                fragment inlineFragment on Dog {
                    ... on Dog {
                        name
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void CorrectTypeOnInlineFragment2()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...inlineFragment2
                    }
                }

                fragment inlineFragment2 on Dog {
                    ... @include(if: true) {
                        name
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void NotOnExistingTypeOnFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...notOnExistingType
                    }
                }

                fragment notOnExistingType on NotInSchema {
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "The type of fragment `notOnExistingType` " +
                    "does not exist in the current schema."));
        }

        [Fact]
        public void NotExistingTypeOnInlineFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...inlineNotExistingType
                    }
                }

                fragment inlineNotExistingType on Dog {
                    ... on NotInSchema {
                        name
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "The specified inline fragment " +
                    "does not exist in the current schema."));
        }
    }
}
