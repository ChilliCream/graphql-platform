using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class OperationNameUniquenessRuleTests
    {
        [Fact]
        public void TwoUniqueQueryOperations()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query getDogName {
                    dog {
                        name
                    }
                }

                query getOwnerName {
                    dog {
                        owner {
                        name
                        }
                    }
                }
            ");

            // act
            OperationNameUniquenessRule validator =
                new OperationNameUniquenessRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
            Assert.Empty(result.Errors);
        }


        [Fact]
        public void TwoQueryOperationsWithTheSameName()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query getName {
                    dog {
                        name
                    }
                }

                query getName {
                    dog {
                        owner {
                            name
                        }
                    }
                }
            ");

            // act
            OperationNameUniquenessRule validator =
                new OperationNameUniquenessRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        $"The operation name `getName` is not unique.",
                        t.Message));
        }

        [Fact]
        public void TwoOperationsWithTheSameName()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query dogOperation {
                    dog {
                        name
                    }
                }

                mutation dogOperation {
                    mutateDog {
                        id
                    }
                }
            ");

            // act
            OperationNameUniquenessRule validator =
                new OperationNameUniquenessRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        $"The operation name `dogOperation` is not unique.",
                        t.Message));
        }
    }
}
