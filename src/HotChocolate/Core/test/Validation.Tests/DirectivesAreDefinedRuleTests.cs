using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Validation
{
    public class DirectivesAreDefinedRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public DirectivesAreDefinedRuleTests()
            : base(services => services.AddDirectiveRules())
        {
        }

        [Fact]
        public void SupportedDirective()
        {
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
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
            // arrange
            ExpectValid(@"
                query Foo($var: Boolean @onVariableDefinition) {
                    name
                }
            ");
        }

        [Fact]
        public void WithMisplacedDirectiveOnQuery()
        {
            // arrange
            ExpectErrors(@"
                query Foo($var: Boolean) @include(if: true) {
                    name   
                } 
            ");
        }
        [Fact]
        public void WithMisplacedDirectivesOnField()
        {
            // arrange
            ExpectErrors(@"
                 query Foo($var: Boolean)  {
                    name @onQuery   
                } 
            ");
        }
        [Fact]
        public void WithMisplacedDirectivesOnFieldRepeatedly()
        {
            // arrange
            ExpectErrors(@"
                 query Foo($var: Boolean)  {
                    name @onQuery @include(if: $var) 
                } 
            ");
        }
        [Fact]
        public void WithMisplacedDirectivesOnMutation()
        {
            // arrange
            ExpectErrors(@" 
                mutation Bar @onQuery {
                    someField
                }
            ");
        }

        [Fact]
        public void WithMisplacedDirectivesOnSubscription()
        {
            // arrange
            ExpectErrors(@" 
                subscription Bar @onQuery {
                    someField
                }
            ");
        }

        [Fact]
        public void WithMisplacedDirectivesOnVariableDefinition()
        {
            // arrange
            ExpectErrors(@"
                query Foo($var: Boolean @onQuery(if: true))  {
                    name  
                } 
            ");
        }

        [Fact]
        public void WithMisplacedDirectivesOnFragemnt()
        {
            // arrange
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
            // arrange
            ExpectErrors(@"
                query Foo($var: Boolean @onField) {
                    name
                }
            ");
        }
    }
}
