using HotChocolate.Execution.Validation;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Execution.Validation
{
    public class OperationNameUniquenessValidatorTests
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
            OperationNameUniquenessValidator validator =
                new OperationNameUniquenessValidator();
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
            OperationNameUniquenessValidator validator =
                new OperationNameUniquenessValidator();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
            Assert.Empty(result.Errors);
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
            OperationNameUniquenessValidator validator =
                new OperationNameUniquenessValidator();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
            Assert.Empty(result.Errors);
        }
    }
}
