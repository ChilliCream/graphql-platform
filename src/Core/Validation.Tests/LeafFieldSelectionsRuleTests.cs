using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class LeafFieldSelectionsRuleTests
           : ValidationTestBase
    {
        public LeafFieldSelectionsRuleTests()
            : base(new LeafFieldSelectionsRule())
        {
        }

        [Fact]
        public void ScalarSelection()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        barkVolume
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void StringList()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    stringList
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ScalarSelectionsNotAllowedOnInt()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        barkVolume {
                            sinceWhen
                        }
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "`barkVolume` is a scalar field. Selections on scalars " +
                    "or enums are never allowed, because they are the leaf " +
                    "nodes of any GraphQL query."));
        }

        [Fact]
        public void DirectQueryOnObjectWithoutSubFields()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query directQueryOnObjectWithoutSubFields {
                    human
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "`human` is an object, interface or union type " +
                    "field. Leaf selections on objects, interfaces, and " +
                    "unions without subfields are disallowed."));
        }

        [Fact]
        public void DirectQueryOnInterfaceWithoutSubFields()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query directQueryOnInterfaceWithoutSubFields {
                    pet
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "`pet` is an object, interface or union type " +
                    "field. Leaf selections on objects, interfaces, and " +
                    "unions without subfields are disallowed."));
        }

        [Fact]
        public void DirectQueryOnUnionWithoutSubFields()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query directQueryOnUnionWithoutSubFields {
                    catOrDog
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "`catOrDog` is an object, interface or union type " +
                    "field. Leaf selections on objects, interfaces, and " +
                    "unions without subfields are disallowed."));
        }
    }
}
