using ChilliCream.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Validation
{
    public class FieldSelectionMergingRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public FieldSelectionMergingRuleTests()
            : base(builder => builder.AddFieldRules())
        {
        }

        [Fact]
        public void MergeIdenticalFields()
        {
            ExpectValid(@"
                {
                    dog {
                        ... mergeIdenticalFields
                    }
                }

                fragment mergeIdenticalFields on Dog {
                    name
                    name
                }
            ");
        }

        [Fact]
        public void MergeIdenticalAliasesAndFields()
        {
            ExpectValid(@"
                {
                    dog {
                        ... mergeIdenticalAliasesAndFields
                    }
                }

                fragment mergeIdenticalAliasesAndFields on Dog {
                    otherName: name
                    otherName: name
                }
            ");
        }

        [Fact]
        public void ConflictingBecauseAlias()
        {
            ExpectErrors(@"
                {
                    dog {
                        ... conflictingBecauseAlias
                    }
                }

                fragment conflictingBecauseAlias on Dog {
                    name: nickname
                    name
                }
            ",
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
        }

        [Fact]
        public void MergeIdenticalFieldsWithIdenticalArgs()
        {
            ExpectValid(@"
                {
                    dog {
                        ... mergeIdenticalFieldsWithIdenticalArgs
                    }
                }

                fragment mergeIdenticalFieldsWithIdenticalArgs on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand(dogCommand: SIT)
                }
            ");
        }

        [Fact]
        public void MergeIdenticalFieldsWithIdenticalValues()
        {
            ExpectValid(@"
                {
                    dog {
                        ... mergeIdenticalFieldsWithIdenticalValues
                    }
                }

                fragment mergeIdenticalFieldsWithIdenticalValues on Dog {
                    doesKnowCommand(dogCommand: $dogCommand)
                    doesKnowCommand(dogCommand: $dogCommand)
                }
            ");
        }

        [Fact]
        public void ConflictingArgsOnValues()
        {
            ExpectErrors(@"
                {
                    dog {
                        ... conflictingArgsOnValues
                    }
                }

                fragment conflictingArgsOnValues on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand(dogCommand: HEEL)
                }
            ",
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
        }

        [Fact]
        public void ConflictingArgsValueAndVar()
        {
            ExpectErrors(@"
                query($dogCommand: DogCommand!) {
                    dog {
                        ... conflictingArgsValueAndVar
                    }
                }

                fragment conflictingArgsValueAndVar on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand(dogCommand: $dogCommand)
                }
            ",
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
        }

        [Fact]
        public void ConflictingArgsWithVars()
        {
            ExpectErrors(@"
                query($varOne: DogCommand! $varTwo: DogCommand!) {
                    dog {
                        ... conflictingArgsWithVars
                    }
                }

                fragment conflictingArgsWithVars on Dog {
                    doesKnowCommand(dogCommand: $varOne)
                    doesKnowCommand(dogCommand: $varTwo)
                }
            ",
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
        }

        [Fact]
        public void DifferingArgs()
        {
            ExpectErrors(@"
                {
                    dog {
                        ... differingArgs
                    }
                }

                fragment differingArgs on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand
                }
            ",
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
        }

        [Fact]
        public void SafeDifferingFields()
        {
            ExpectValid(@"
                {
                    catOrDog {
                        ... safeDifferingFields
                    }
                }

                fragment safeDifferingFields on Pet {
                    ... on Dog {
                        volume: barkVolume
                    }
                    ... on Cat {
                        volume: meowVolume
                    }
                }
            ");
        }

        [Fact]
        public void SafeDifferingArgs()
        {
            ExpectValid(@"
                {
                    dog {
                        ... safeDifferingArgs
                    }
                }

                fragment safeDifferingArgs on Pet {
                    ... on Dog {
                        doesKnowCommand(dogCommand: SIT)
                    }
                    ... on Cat {
                        doesKnowCommand(catCommand: JUMP)
                    }
                }
            ");
        }

        [Fact]
        public void ConflictingDifferingResponses()
        {
            ExpectErrors(@"
                {
                    dog {
                        ... conflictingDifferingResponses
                    }
                }

                fragment conflictingDifferingResponses on Pet {
                    ... on Dog {
                        someValue: nickname
                    }
                    ... on Cat {
                        someValue: meowVolume
                    }
                }
            ",
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
        }

        [Fact]
        public void ShortHandQueryWithNoDuplicateFields()
        {
            ExpectValid(
                @"{
                    __type (type: ""Foo"") {
                        name
                        fields {
                            name
                            type {
                                name
                            }
                        }
                    }
                }");
        }

        [Fact]
        public void ShortHandQueryWithDuplicateFieldInSecondLevelFragment()
        {
            ExpectErrors(@"
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
            ",
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
        }


        [Fact]
        public void ShortHandQueryWithDupMergableFieldInSecondLevelFragment()
        {
            // arrange
            ExpectValid(@"
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
        }

        [Fact]
        public void TypeNameFieldOnInterfaceIsMergable()
        {
            // arrange
            ExpectValid(@"
                {
                    dog {
                        ... interfaceFieldSelection
                    }
                }

                fragment interfaceFieldSelection on Pet {
                    __typename
                    __typename
                }
            ");
        }

        [Fact]
        public void TypeNameFieldOnUnionIsMergable()
        {
            ExpectValid(@"
                {
                    catOrDog {
                        ... interfaceFieldSelection
                    }
                }

                fragment interfaceFieldSelection on CatOrDog {
                    __typename
                    __typename
                }
            ");
        }

        [Fact]
        public void TypeNameFieldOnObjectIsMergable()
        {
            ExpectValid(@"
                fragment interfaceFieldSelection on Cat {
                    __typename
                    __typename
                }
            ");
        }

        [Fact]
        public void InvalidFieldsShouldNotRaiseValidationError()
        {
            ExpectValid(FileResource.Open("InvalidIntrospectionQuery.graphql"));
        }
    }
}
