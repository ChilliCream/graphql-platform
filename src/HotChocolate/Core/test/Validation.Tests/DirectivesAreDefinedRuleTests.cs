using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class DirectivesAreDefinedRuleTests
    : DocumentValidatorVisitorTestBase
{
    public DirectivesAreDefinedRuleTests()
        : base(builder => builder.AddDirectiveRules())
    {
    }

    [Fact]
    public void SupportedDirective()
    {
        ExpectValid(@"
                {
                    dog {
                        name @skip(if: true)
                    }
                }
            ");
    }

    [Fact]
    public void UnsupportedDirective()
    {
        ExpectErrors(@"
                {
                    dog {
                        name @foo(bar: true)
                    }
                }
            ",
            t => Assert.Equal(
                "The specified directive `foo` " +
                "is not supported by the current schema.",
                t.Message));
    }

    [Fact]
    public void SkipDirectiveIsInTheWrongPlace()
    {
        ExpectErrors(@"
                query @skip(if: $foo) {
                    field
                }
            ",
            t => Assert.Equal(
                "The specified directive is not valid the " +
                "current location.", t.Message));
    }

    [Fact]
    public void SkipDirectiveIsInTheRightPlace()
    {
        ExpectValid(@"
                query a {
                    field @skip(if: $foo)
                }
            ");
    }

    [Fact]
    public void DuplicateSkipDirectives()
    {
        ExpectErrors(@"
                query ($foo: Boolean = true, $bar: Boolean = false) {
                    field @skip(if: $foo) @skip(if: $bar)
                }
            ",
            t => Assert.Equal(
                "Only one of each directive is allowed per location.",
                t.Message));
    }

    [Fact]
    public void SkipOnTwoDifferentFields()
    {
        ExpectValid(@"
                query ($foo: Boolean = true, $bar: Boolean = false) {
                    field @skip(if: $foo) {
                        subfieldA
                    }
                    field @skip(if: $bar) {
                        subfieldB
                    }
                }
            ");
    }

    [Fact]
    public void WithNoDirectives()
    {
        ExpectValid(@"
                query Foo {
                    name
                    ...Frag
                }
                fragment Frag on Dog {
                    name
                }
            ");
    }

    [Fact]
    public void WithKnownDirectives()
    {
        ExpectValid(@"
                 {
                    dog @include(if: true) {
                        name
                    }
                    human @skip(if: false) {
                        name
                    }
                }
            ");
    }

    [Fact]
    public void WithUnknownDirectives()
    {
        ExpectErrors(@"
                 {
                    dog @unknown(directive: ""value"") {
                        name
                    }
                }
            ");
    }

    [Fact]
    public void WithManyUnknownDirectives()
    {
        ExpectErrors(@"
                {
                    dog @unknown(directive: ""value"") {
                        name
                    }
                    human @unknown(directive: ""value"") {
                        name
                        pets @unknown(directive: ""value"") {
                            name
                        }
                    }
                }
            ");
    }

    [Fact]
    public void WithWellPlacedDirectives()
    {
        ExpectValid(@"
                query ($var: Boolean) @onQuery {
                    name @include(if: $var)
                    ...Frag @include(if: true)
                    skippedField @skip(if: true)
                    ...SkippedFrag @skip(if: true)
                    ... @skip(if: true) {
                    skippedField
                    }
                }

                mutation @onMutation {
                    someField
                }

                subscription @onSubscription {
                    someField
                }

                fragment Frag on SomeType @onFragmentDefinition {
                    someField
                }
            ");
    }

    [Fact]
    public void WithWellPlacedVariableDefinitionDirective()
    {
        ExpectValid(@"
                query Foo($var: Boolean @onVariableDefinition) {
                    name
                }
            ");
    }

    [Fact]
    public void WithMisplacedDirectiveOnQuery()
    {
        ExpectErrors(@"
                query Foo($var: Boolean) @include(if: true) {
                    name
                }
            ");
    }

    [Fact]
    public void WithMisplacedDirectivesOnField()
    {
        ExpectErrors(@"
                 query Foo($var: Boolean)  {
                    name @onQuery
                }
            ");
    }

    [Fact]
    public void WithMisplacedDirectivesOnFieldRepeatedly()
    {
        ExpectErrors(@"
                 query Foo($var: Boolean)  {
                    name @onQuery @include(if: $var)
                }
            ");
    }

    [Fact]
    public void WithMisplacedDirectivesOnMutation()
    {
        ExpectErrors(@"
                mutation Bar @onQuery {
                    someField
                }
            ");
    }

    [Fact]
    public void WithMisplacedDirectivesOnSubscription()
    {
        ExpectErrors(@"
                subscription Bar @onQuery {
                    someField
                }
            ");
    }

    [Fact]
    public void WithMisplacedDirectivesOnVariableDefinition()
    {
        ExpectErrors(@"
                query Foo($var: Boolean @onQuery(if: true))  {
                    name
                }
            ");
    }

    [Fact]
    public void WithMisplacedDirectivesOnFragemnt()
    {
        ExpectErrors(@"
                 query Foo($var: Boolean)  {
                    ...Frag @onQuery
                }
                fragment Frag on Query  {
                    name
                }
            ");
    }

    [Fact]
    public void WithMisplacedVariableDefinitionDirective()
    {
        ExpectErrors(@"
                query Foo($var: Boolean @onField) {
                    name
                }
            ");
    }

    [Fact]
    public void NoDirectives()
    {
        ExpectValid(@"
                {
                    ...Test
                }
                fragment Test on Query {
                    name
                }
            ");
    }

    [Fact]
    public void UniqueDirectivesInDifferentLocations()
    {
        ExpectValid(@"
                {
                    ...Test
                }
                fragment Test on Query @directiveA {
                    field @directiveB
                }
            ");
    }

    [Fact]
    public void UniqueDirectivesInSameLocations()
    {
        ExpectValid(@"
                {
                    ...Test
                }
                fragment Test on Query @directiveA @directiveB {
                    field @directiveA @directiveB
                }
            ");
    }

    [Fact]
    public void SameDirectivesInDifferentLocations()
    {
        ExpectValid(@"
                {
                    ...Test
                }
                fragment Test on Query @directiveA {
                    field @directiveA
                }
            ");
    }

    [Fact]
    public void SameDirectivesInSimilarLocations()
    {
        ExpectValid(@"
                {
                    ...Test
                }
                fragment Test on Query {
                    field @directiveA
                    field @directiveA
                }
            ");
    }

    [Fact]
    public void RepeatableDirectivesInSameLocation()
    {
        ExpectValid(@"
                {
                    ...Test
                }
                fragment Test on Query @repeatable @repeatable {
                    field @repeatable @repeatable
                }
            ");
    }

    [Fact]
    public void DuplicateDirectivesInOneLocation()
    {
        ExpectErrors(@"
                {
                    ...Test
                }
                fragment Test on Query {
                    field @directiveA @directiveA
                }
            ");
    }

    [Fact]
    public void ManyDuplicateDirectivesInOneLocation()
    {
        ExpectErrors(@"
                {
                    ...Test
                }
                fragment Test on Query {
                    field @directiveA @directiveA @directiveA
                }
            ");
    }

    [Fact]
    public void DifferentDuplicateDirectivesInOneLocation()
    {
        ExpectErrors(@"
                {
                    ...Test
                }
                fragment Test on Query {
                    field @directiveA @directiveB @directiveA @directiveB
                }
            ");
    }

    [Fact]
    public void DuplicateDirectivesInManyLocations()
    {
        ExpectErrors(@"
                {
                    ...Test
                }
                fragment Test on Query @directiveA @directiveA {
                    field @directiveA @directiveA
                }
            ");
    }
}
