using Xunit;

namespace HotChocolate.Validation
{
    public class LeafFieldSelectionsRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public LeafFieldSelectionsRuleTests()
            : base(services => services.AddFieldRules())
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
            // arrange
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
            // arrange
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
    }
}
