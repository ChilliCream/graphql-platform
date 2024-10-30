using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class LeafFieldSelectionsRuleTests
    : DocumentValidatorVisitorTestBase
{
    public LeafFieldSelectionsRuleTests()
        : base(builder => builder.AddFieldRules())
    {
    }

    [Fact]
    public void ScalarSelection()
    {
        ExpectValid(@"
                {
                    dog {
                        barkVolume
                    }
                }
            ");
    }

    [Fact]
    public void StringList()
    {
        ExpectValid(@"
                {
                    stringList
                }
            ");
    }

    [Fact]
    public void ScalarSelectionsNotAllowedOnInt()
    {
        ExpectErrors(@"
                {
                    dog {
                        barkVolume {
                            sinceWhen
                        }
                    }
                }
            ",
            t => Assert.Equal(
                "Field \"barkVolume\" must not have a selection since type \"Int\" has no " +
                "subfields.",
                t.Message));
    }

    [Fact]
    public void DirectQueryOnObjectWithoutSubFields()
    {
        ExpectErrors(@"
                query directQueryOnObjectWithoutSubFields {
                    human
                }
            ",
            t => Assert.Equal(
                "Field \"human\" of type \"Human\" must have a selection of subfields. Did you " +
                "mean \"human { ... }\"?",
                t.Message));
    }

    [Fact]
    public void DirectQueryOnObjectWithoutSubFieldsEmptySelection()
    {
        ExpectErrors(@"
                query directQueryOnObjectWithoutSubFields {
                    human {}
                }
            ",
            t => Assert.Equal(
                "Field \"human\" of type \"Human\" must have a selection of subfields. Did you " +
                "mean \"human { ... }\"?",
                t.Message));
    }

    [Fact]
    public void DirectQueryOnInterfaceWithoutSubFields()
    {
        ExpectErrors(@"
                query directQueryOnInterfaceWithoutSubFields {
                    pet
                }
            ",
            t => Assert.Equal(
                "Field \"pet\" of type \"Human\" must have a selection of subfields. Did you mean " +
                "\"pet { ... }\"?",
                t.Message));
    }

    [Fact]
    public void DirectQueryOnInterfaceWithoutSubFieldsEmptySelection()
    {
        ExpectErrors(@"
                query directQueryOnInterfaceWithoutSubFields {
                    pet {}
                }
            ",
            t => Assert.Equal(
                "Field \"pet\" of type \"Human\" must have a selection of subfields. Did you mean " +
                "\"pet { ... }\"?",
                t.Message));
    }

    [Fact]
    public void DirectQueryOnUnionWithoutSubFields()
    {
        ExpectErrors(@"
                query directQueryOnUnionWithoutSubFields {
                    catOrDog
                }
            ",
            t => Assert.Equal(
                "Field \"catOrDog\" of type \"CatOrDog\" must have a selection of subfields. Did " +
                "you mean \"catOrDog { ... }\"?",
                t.Message));
    }

    [Fact]
    public void DirectQueryOnUnionWithoutSubFieldsEmptySelection()
    {
        ExpectErrors(@"
                query directQueryOnUnionWithoutSubFields {
                    catOrDog {}
                }
            ",
            t => Assert.Equal(
                "Field \"catOrDog\" of type \"CatOrDog\" must have a selection of subfields. Did " +
                "you mean \"catOrDog { ... }\"?",
                t.Message));
    }

    [Fact]
    public void InterfaceTypeMissingSelection()
    {
        ExpectErrors(@"
                {
                    human { pets }
                }
            ",
            t => Assert.Equal(
                "Field \"pets\" of type \"[Pet]\" must have a selection of subfields. Did you " +
                "mean \"pets { ... }\"?",
                t.Message));
    }

    [Fact]
    public void InterfaceTypeMissingSelectionEmptySelection()
    {
        ExpectErrors(@"
                {
                    human { pets {} }
                }
            ",
            t => Assert.Equal(
                "Field \"pets\" of type \"[Pet]\" must have a selection of subfields. Did you " +
                "mean \"pets { ... }\"?",
                t.Message));
    }

    [Fact]
    public void EmptyQueryType()
    {
        ExpectErrors(@"
                { }
            ",
            t => Assert.Equal(
                "Operation `Unnamed` has a empty selection set. Root types without " +
                "subfields are disallowed.",
                t.Message));
    }

    [Fact]
    public void EmptyNamedQueryType()
    {
        ExpectErrors(@"
                query Foo { }
            ",
            t => Assert.Equal(
                "Operation `Foo` has a empty selection set. Root types without " +
                "subfields are disallowed.",
                t.Message));
    }

    [Fact]
    public void EmptyMutationType()
    {
        ExpectErrors(@"
                mutation { }
            ",
            t => Assert.Equal(
                "Operation `Unnamed` has a empty selection set. Root types without " +
                "subfields are disallowed.",
                t.Message));
    }

    [Fact]
    public void EmptyNamedMutationType()
    {
        ExpectErrors(@"
                mutation Foo { }
            ",
            t => Assert.Equal(
                "Operation `Foo` has a empty selection set. Root types without " +
                "subfields are disallowed.",
                t.Message));
    }

    [Fact]
    public void EmptySubscriptionType()
    {
        ExpectErrors(@"
                subscription { }
            ",
            t => Assert.Equal(
                "Operation `Unnamed` has a empty selection set. Root types without " +
                "subfields are disallowed.",
                t.Message));
    }

    [Fact]
    public void EmptyNamedSubscriptionType()
    {
        ExpectErrors(@"
                subscription Foo { }
            ",
            t => Assert.Equal(
                "Operation `Foo` has a empty selection set. Root types without " +
                "subfields are disallowed.",
                t.Message));
    }

    [Fact]
    public void ScalarSelectionNotAllowedOnBoolean()
    {
        ExpectErrors(@"
                {
                    dog {
                        barks {
                            sinceWhen
                        }
                    }
                }
            ",
            t => Assert.Equal(
                "Field \"barks\" must not have a selection since type \"Boolean!\" has no " +
                "subfields.",
                t.Message));
    }

    [Fact]
    public void ScalarSelectionNotAllowedOnEnum()
    {
        ExpectErrors(@"
                {
                    catOrDog {
                        ... on Cat {
                            furColor {
                                inHexDec
                            }
                        }
                    }
                }
            ",
            t => Assert.Equal(
                "Field \"furColor\" must not have a selection since type \"FurColor\" has no " +
                "subfields.",
                t.Message));
    }

    [Fact]
    public void ScalarSelectionNotAllowedOnListOfScalars()
    {
        ExpectErrors(@"
                {
                    listOfScalars {
                        x
                    }
                }
            ",
            t => Assert.Equal(
                "Field \"listOfScalars\" must not have a selection since type \"[String]\" has " +
                "no subfields.",
                t.Message));
    }

    [Fact]
    public void ScalarSelectionNotAllowedOnListOfListOfScalars()
    {
        ExpectErrors(@"
                {
                    listOfListOfScalars {
                        x
                    }
                }
            ",
            t => Assert.Equal(
                "Field \"listOfListOfScalars\" must not have a selection since type " +
                "\"[[String]]\" has no subfields.",
                t.Message));
    }

    [Fact]
    public void ScalarSelectionNotAllowedWithArgs()
    {
        ExpectErrors(@"
                {
                    dog {
                        doesKnowCommand(dogCommand: SIT) { sinceWhen }
                    }
                }
            ",
            t => Assert.Equal(
                "Field \"doesKnowCommand\" must not have a selection since type \"Boolean!\" has " +
                "no subfields.",
                t.Message));
    }

    [Fact]
    public void ScalarSelectionNotAllowedWithDirectives()
    {
        ExpectErrors(@"
                {
                    dog {
                        name @include(if: true) { isAlsoHumanName }
                    }
                }
            ",
            t => Assert.Equal(
                "Field \"name\" must not have a selection since type \"String!\" has no subfields.",
                t.Message));
    }

    [Fact]
    public void ScalarSelectionNotAllowedWithDirectivesAndArgs()
    {
        ExpectErrors(@"
                {
                    dog {
                        doesKnowCommand(dogCommand: SIT) @include(if: true) { sinceWhen }
                    }
                }
            ",
            t => Assert.Equal(
                "Field \"doesKnowCommand\" must not have a selection since type \"Boolean!\" has " +
                "no subfields.",
                t.Message));
    }
}
