﻿using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Validation
{
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
                "`barkVolume` returns a scalar value. Selections on scalars " +
                "or enums are never allowed, because they are the leaf " +
                "nodes of any GraphQL query.",
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
                "`human` is an object, interface or union type " +
                "field. Leaf selections on objects, interfaces, and " +
                "unions without subfields are disallowed.",
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
                "`pet` is an object, interface or union type " +
                "field. Leaf selections on objects, interfaces, and " +
                "unions without subfields are disallowed.",
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
                "`catOrDog` is an object, interface or union type " +
                "field. Leaf selections on objects, interfaces, and " +
                "unions without subfields are disallowed.",
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
                "`pets` is an object, interface or union type " +
                "field. Leaf selections on objects, interfaces, and " +
                "unions without subfields are disallowed.",
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
                "`barks` returns a scalar value. Selections on scalars " +
                "or enums are never allowed, because they are the leaf " +
                "nodes of any GraphQL query.",
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
                "`furColor` returns an enum value. Selections on scalars " +
                "or enums are never allowed, because they are the leaf " +
                "nodes of any GraphQL query.",
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
                "`doesKnowCommand` returns a scalar value. Selections on scalars " +
                "or enums are never allowed, because they are the leaf " +
                "nodes of any GraphQL query.",
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
                "`name` returns a scalar value. Selections on scalars " +
                "or enums are never allowed, because they are the leaf " +
                "nodes of any GraphQL query.",
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
                "`doesKnowCommand` returns a scalar value. Selections on scalars " +
                "or enums are never allowed, because they are the leaf " +
                "nodes of any GraphQL query.",
                t.Message));
        }
    }
}
