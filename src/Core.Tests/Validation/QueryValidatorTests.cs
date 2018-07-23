using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class QueryValidatorTests
    {
        [Fact]
        public void SchemaIsNulll()
        {
            // act
            Action a = () => new QueryValidator(null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void QueryIsNull()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = new QueryValidator(schema);

            // act
            // act
            Action a = () => queryValidator.Validate(null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void QueryWithTypeSystemDefinitions()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                query getDogName {
                    dog {
                        name
                        color
                    }
                }

                extend type Dog {
                    color: String
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = new QueryValidator(schema);

            // act
            QueryValidationResult result = queryValidator.Validate(query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "A document containing TypeSystemDefinition " +
                    "is invalid for execution.", t.Message));
        }

        [Fact]
        public void QueryWithOneAnonymousAndOneNamedOperation()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
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

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = new QueryValidator(schema);

            // act
            QueryValidationResult result = queryValidator.Validate(query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t =>
                {
                    Assert.Equal(
                        "GraphQL allows a short‐hand form for defining query " +
                        "operations when only that one operation exists in the " +
                        "document.", t.Message);
                });
        }

        [Fact]
        public void TwoQueryOperationsWithTheSameName()
        {
            // arrange
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

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = new QueryValidator(schema);

            // act
            QueryValidationResult result = queryValidator.Validate(query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        $"The operation name `getName` is not unique.",
                        t.Message));
        }

        [Fact]
        public void OperationWithTwoVariablesThatHaveTheSameName()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                query houseTrainedQuery($atOtherHomes: Boolean, $atOtherHomes: Boolean) {
                    dog {
                        isHousetrained(atOtherHomes: $atOtherHomes)
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = new QueryValidator(schema);

            // act
            QueryValidationResult result = queryValidator.Validate(query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "A document containing operations that " +
                    "define more than one variable with the same " +
                    "name is invalid for execution.", t.Message),
                t => Assert.Equal(
                    "The field `isHousetrained` does not exist " +
                    "on the type `Dog`.", t.Message));
        }

        [Fact]
        public void DuplicateArgument()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                fragment goodNonNullArg on Arguments {
                    nonNullBooleanArgField(nonNullBooleanArg: true, nonNullBooleanArg: true)
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = new QueryValidator(schema);

            // act
            QueryValidationResult result = queryValidator.Validate(query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"Arguments are not unique.", t.Message));
        }

        [Fact]
        public void MissingRequiredArgNonNullBooleanArg()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                fragment missingRequiredArg on Arguments {
                    nonNullBooleanArgField(nonNullBooleanArg: null)
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = new QueryValidator(schema);

            // act
            QueryValidationResult result = queryValidator.Validate(query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"The argument `nonNullBooleanArg` is required " +
                    "and does not allow null values.", t.Message));
        }

        [Fact]
        public void DisallowedSecondRootField()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                subscription sub {
                    newMessage {
                        body
                        sender
                    }
                    disallowedSecondRootField
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = new QueryValidator(schema);

            // act
            QueryValidationResult result = queryValidator.Validate(query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"Subscription operation `sub` must " +
                    "have exactly one root field.", t.Message),
                t => Assert.Equal(
                    "The field `disallowedSecondRootField` does not exist " +
                    "on the type `Subscription`.", t.Message));
        }

        [Fact]
        public void FieldIsNotDefinedOnTypeInFragment()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                fragment fieldNotDefined on Dog {
                    meowVolume
                }

                fragment aliasedLyingFieldTargetNotDefined on Dog {
                    barkVolume: kawVolume
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = new QueryValidator(schema);

            // act
            QueryValidationResult result = queryValidator.Validate(query);

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
        public void VariableNotUsedWithinFragment()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                query variableNotUsedWithinFragment($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedWithoutVariableFragment
                    }
                }

                fragment isHousetrainedWithoutVariableFragment on Dog {
                    barkVolume
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = new QueryValidator(schema);

            // act
            QueryValidationResult result = queryValidator.Validate(query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The following variables were not used: " +
                    "atOtherHomes.", t.Message));
        }
    }
}
