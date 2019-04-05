using System.Linq;
using Xunit;

namespace HotChocolate.Types
{
    public class SpecedDirectiveTypeTests
        : TypeTestBase
    {
        [Fact]
        public void EnsureSkipDirectiveIsAvailable()
        {
            // arrange
            ISchema schema = CreateSchema(b => { });

            // act
            DirectiveType directive =
                schema.DirectiveTypes.FirstOrDefault(
                    t => t.Name.Equals("skip"));

            // assert
            Assert.NotNull(directive);
            Assert.IsType<SkipDirectiveType>(directive);
            Assert.Equal("skip", directive.Name);
            Assert.Collection(directive.Arguments,
                t =>
                {
                    Assert.Equal("if", t.Name);
                    Assert.IsType<NonNullType>(t.Type);
                    Assert.IsType<BooleanType>(((NonNullType)t.Type).Type);
                });
            Assert.Collection(directive.Locations,
                t => Assert.Equal(DirectiveLocation.Field, t),
                t => Assert.Equal(DirectiveLocation.FragmentSpread, t),
                t => Assert.Equal(DirectiveLocation.InlineFragment, t));
        }

        [Fact]
        public void EnsureIncludeDirectiveIsAvailable()
        {
            // arrange
            ISchema schema = CreateSchema(b => { });

            // act
            DirectiveType directive =
                schema.DirectiveTypes.FirstOrDefault(
                    t => t.Name.Equals("include"));

            // assert
            Assert.NotNull(directive);
            Assert.IsType<IncludeDirectiveType>(directive);
            Assert.Equal("include", directive.Name);
            Assert.Collection(directive.Arguments,
                t =>
                {
                    Assert.Equal("if", t.Name);
                    Assert.IsType<NonNullType>(t.Type);
                    Assert.IsType<BooleanType>(((NonNullType)t.Type).Type);
                });
            Assert.Collection(directive.Locations,
                t => Assert.Equal(DirectiveLocation.Field, t),
                t => Assert.Equal(DirectiveLocation.FragmentSpread, t),
                t => Assert.Equal(DirectiveLocation.InlineFragment, t));
        }
    }
}
