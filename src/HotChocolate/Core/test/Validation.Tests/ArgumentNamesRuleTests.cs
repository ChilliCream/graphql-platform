using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class ArgumentNamesRuleTests
    : DocumentValidatorVisitorTestBase
{
    public ArgumentNamesRuleTests()
        : base(builder => builder.AddArgumentRules())
    {
    }

    [Fact]
    public void ArgOnRequiredArg()
    {
        ExpectValid(@"
                query {
                    dog {
                        ... argOnRequiredArg
                    }
                }

                fragment argOnRequiredArg on Dog {
                    doesKnowCommand(dogCommand: SIT)
                }
            ");
    }

    [Fact]
    public void ArgOnOptional()
    {
        ExpectValid(@"
                query {
                    dog {
                        ... argOnOptional
                    }
                }

                fragment argOnOptional on Dog {
                    isHouseTrained(atOtherHomes: true) @include(if: true)
                }
            ");
    }

    [Fact]
    public void InvalidFieldArgName()
    {
        ExpectErrors(@"
                query {
                    dog {
                        ... invalidArgName
                    }
                }

                fragment invalidArgName on Dog {
                    doesKnowCommand(command: CLEAN_UP_HOUSE)
                }
            ",
            t => Assert.Equal(
                $"The argument `command` does not exist.", t.Message),
            t => Assert.Equal(
                $"The argument `dogCommand` is required.", t.Message));
    }

    [Fact]
    public void InvalidDirectiveArgName()
    {
        ExpectErrors(@"
                query {
                    dog {
                        ... invalidArgName
                    }
                }

                fragment invalidArgName on Dog {
                    isHouseTrained(atOtherHomes: true) @include(unless: false)
                }
            ",
            t => Assert.Equal(
                $"The argument `unless` does not exist.", t.Message),
            t => Assert.Equal(
                $"The argument `if` is required.", t.Message));
    }

    [Fact]
    public void ArgumentOrderDoesNotMatter()
    {
        ExpectValid(@"
                query {
                    arguments {
                        ... multipleArgs
                        ... multipleArgsReverseOrder
                    }
                }

                fragment multipleArgs on Arguments {
                    multipleReqs(x: 1, y: 2)
                }

                fragment multipleArgsReverseOrder on Arguments {
                    multipleReqs(y: 1, x: 2)
                }
            ");
    }

    [Fact]
    public void ArgsAreKnowDeeply()
    {
        ExpectValid(@"
                {
                    dog {
                        doesKnowCommand(dogCommand: SIT)
                    }
                    human {
                        pets {
                            ... on Dog {
                                doesKnowCommand(dogCommand: SIT)
                            }
                        }
                    }
                }
            ");
    }

    [Fact]
    public void DirectiveArgsAreKnown()
    {
        ExpectValid(@"
                {
                    dog @skip(if: true)
                }
            ");
    }

    [Fact]
    public void DirectiveWithoutArgsIsValid()
    {
        ExpectValid(@"
                {
                    dog @complex
                }
            ");
    }

    [Fact]
    public void DirectiveWithWrongArgsIsInvalid()
    {
        ExpectErrors(@"
                {
                    dog @complex(if:false)
                }
            ");
    }

    [Fact]
    public void MisspelledDirectiveArgsAreReported()
    {
        ExpectErrors(@"
                {
                    dog @skip(iff: true)
                }
            ");
    }

    [Fact]
    public void MisspelledFieldArgsAreReported()
    {
        ExpectErrors(@"
                query {
                    dog {
                        ... invalidArgName
                    }
                }
                fragment invalidArgName on Dog {
                    doesKnowCommand(DogCommand: true)
                }
            ");
    }

    [Fact]
    public void UnknownArgsAmongstKnowArgs()
    {
        ExpectErrors(@"
                query {
                    dog {
                        ... oneGoodArgOneInvalidArg
                    }
                }
                fragment oneGoodArgOneInvalidArg on Dog {
                    doesKnowCommand(whoKnows: 1, dogCommand: SIT, unknown: true)
                }
            ");
    }

    [Fact]
    public void UnknownArgsDeeply()
    {
        ExpectErrors(@"
                {
                    dog {
                        doesKnowCommand(unknown: true)
                    }
                    human {
                    pet {
                        ... on Dog {
                                doesKnowCommand(unknown: true)
                            }
                        }
                    }
                }
            ");
    }

    [Fact]
    public void NoArgumentsOnField()
    {
        // arrange
        ExpectValid(@"
                {
                    fieldWithArg
                }
            ");
    }

    [Fact]
    public void NoArgumentsOnDirective()
    {
        // arrange
        ExpectValid(@"
                {
                    fieldWithArg @directive
                }
            ");
    }

    [Fact]
    public void ArgumentOnField()
    {
        // arrange
        ExpectValid(@"
                {
                    fieldWithArg(arg: ""value"")
                }
            ");
    }

    [Fact]
    public void ArgumentOnDirective()
    {
        // arrange
        ExpectValid(@"
                {
                    fieldWithArg @directive(arg: ""value"")
                }
            ");
    }

    [Fact]
    public void SameArgumentOnTwoFields()
    {
        // arrange
        ExpectValid(@"
                {
                    one: fieldWithArg(arg: ""value"")
                    two: fieldWithArg(arg: ""value"")
                }
            ");
    }

    [Fact]
    public void SameArgumentOnFieldAndDirective()
    {
        // arrange
        ExpectValid(@"
                {
                    fieldWithArg(arg: ""value"") @directive(arg: ""value"")
                }
            ");
    }

    [Fact]
    public void SameArgumentOnTwoDirectives()
    {
        // arrange
        ExpectValid(@"
                {
                    fieldWithArg @directive1(arg: ""value"") @directive2(arg: ""value"")
                }
            ");
    }

    [Fact]
    public void MultipleFieldArguments()
    {
        // arrange
        ExpectValid(@"
                {
                fieldWithArg(arg1: ""value"", arg2: ""value"", arg3: ""value"")
                }
            ");
    }

    [Fact]
    public void MultipleDirectiveArguments()
    {
        // arrange
        ExpectValid(@"
                {
                    fieldWithArg @directive(arg1: ""value"", arg2: ""value"", arg3: ""value"")
                }
            ");
    }

    [Fact]
    public void DuplicateFieldArguments()
    {
        // arrange
        ExpectErrors(@"
                {
                    fieldWithArg(arg1: ""value"", arg1: ""value"")
                }
            ");
    }

    [Fact]
    public void ManyDuplicateFieldArguments()
    {
        // arrange
        ExpectErrors(@"
                {
                    fieldWithArg(arg1: ""value"", arg1: ""value"", arg1: ""value"")
                }
            ");
    }

    [Fact]
    public void DuplicateDirectiveArguments()
    {
        // arrange
        ExpectErrors(@"
                {
                    fieldWithArg @directive(arg1: ""value"", arg1: ""value"")
                }
            ");
    }

    [Fact]
    public void ManyDuplicateDirectiveArguments()
    {
        // arrange
        ExpectErrors(@"
                {
                    fieldWithArg @directive(arg1: ""value"", arg1: ""value"", arg1: ""value"")
                }
            ");
    }
}
