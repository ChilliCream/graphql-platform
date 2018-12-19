using ChilliCream.Testing;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class FieldSelectionMergingRuleTests
        : ValidationTestBase
    {
        public FieldSelectionMergingRuleTests()
            : base(new FieldSelectionMergingRule())
        {
        }

        [Fact]
        public void MergeIdenticalFields()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment mergeIdenticalFields on Dog {
                    name
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void MergeIdenticalAliasesAndFields()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment mergeIdenticalAliasesAndFields on Dog {
                    otherName: name
                    otherName: name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ConflictingBecauseAlias()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment conflictingBecauseAlias on Dog {
                    name: nickname
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        "The query has non-mergable fields.",
                        t.Message));
        }

        [Fact]
        public void MergeIdenticalFieldsWithIdenticalArgs()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment mergeIdenticalFieldsWithIdenticalArgs on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand(dogCommand: SIT)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void MergeIdenticalFieldsWithIdenticalValues()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment mergeIdenticalFieldsWithIdenticalValues on Dog {
                    doesKnowCommand(dogCommand: $dogCommand)
                    doesKnowCommand(dogCommand: $dogCommand)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ConflictingArgsOnValues()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment conflictingArgsOnValues on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand(dogCommand: HEEL)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        "The query has non-mergable fields.",
                        t.Message));
        }

        [Fact]
        public void ConflictingArgsValueAndVar()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment conflictingArgsValueAndVar on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand(dogCommand: $dogCommand)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        "The query has non-mergable fields.",
                        t.Message));
        }

        [Fact]
        public void ConflictingArgsWithVars()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment conflictingArgsWithVars on Dog {
                    doesKnowCommand(dogCommand: $varOne)
                    doesKnowCommand(dogCommand: $varTwo)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        "The query has non-mergable fields.",
                        t.Message));
        }

        [Fact]
        public void DifferingArgs()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment differingArgs on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        "The query has non-mergable fields.",
                        t.Message));
        }

        [Fact]
        public void SafeDifferingFields()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment safeDifferingFields on Pet {
                    ... on Dog {
                        volume: barkVolume
                    }
                    ... on Cat {
                        volume: meowVolume
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void SafeDifferingArgs()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment safeDifferingArgs on Pet {
                    ... on Dog {
                        doesKnowCommand(dogCommand: SIT)
                    }
                    ... on Cat {
                        doesKnowCommand(catCommand: JUMP)
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ConflictingDifferingResponses()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment conflictingDifferingResponses on Pet {
                    ... on Dog {
                        someValue: nickname
                    }
                    ... on Cat {
                        someValue: meowVolume
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        "The query has non-mergable fields.",
                        t.Message));
        }

        [Fact]
        public void ShortHandQueryWithNoDuplicateFields()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(
                "{ __type (type: \"Foo\") " +
                "{ name fields { name type { name } } } }");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ShortHandQueryWithDuplicateFieldInSecondLevelFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        doesKnowCommand(dogCommand: DOWN)
                        ... FooLevel1
                    }
                }

                fragment FooLevel1 on Dog {
                    ... FooLevel2
                }

                fragment FooLevel2 on Dog {
                    doesKnowCommand(dogCommand: HEEL)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void ShortHandQueryWithDupMergableFieldInSecondLevelFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        doesKnowCommand(dogCommand: DOWN)
                        ... FooLevel1
                    }
                }

                fragment FooLevel1 on Dog {
                    ... FooLevel2
                }

                fragment FooLevel2 on Dog {
                    doesKnowCommand(dogCommand: DOWN)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void TypeNameFieldOnInterfaceIsMergable()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment interfaceFieldSelection on Pet {
                    __typename
                    __typename
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void TypeNameFieldOnUnionIsMergable()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment interfaceFieldSelection on CatOrDog {
                    __typename
                    __typename
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void TypeNameFieldOnObjectIsMergable()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment interfaceFieldSelection on Cat {
                    __typename
                    __typename
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void InvalidFieldsShouldNotRaiseValidationError()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(
                FileResource.Open("InvalidIntrospectionQuery.graphql"));

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }
    }
}
