using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentsOnCompositeTypesRuleTests
        : ValidationTestBase
    {
        public FragmentsOnCompositeTypesRuleTests()
            : base(new FragmentsOnCompositeTypesRule())
        {
        }

        [Fact]
        public void FragOnObject()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                       ... fragOnObject
                    }
                }

                fragment fragOnObject on Dog {
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void FragOnInterface()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                       ... fragOnInterface
                    }
                }

                fragment fragOnInterface on Pet {
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void FragOnUnion()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                       ... fragOnUnion
                    }
                }

                fragment fragOnUnion on CatOrDog {
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
        public void FragOnScalar()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                       ... fragOnScalar
                    }
                }

                fragment fragOnScalar on Int {
                    something
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "Fragments can only be declared on unions, interfaces, " +
                    "and objects."));
        }

        [Fact]
        public void InlineFragOnScalar()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                       ... inlineFragOnScalar
                    }
                }

                fragment inlineFragOnScalar on Dog {
                    ... on Boolean {
                        somethingElse
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "Fragments can only be declared on unions, interfaces, " +
                    "and objects."));
        }
    }
}
