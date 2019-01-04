using System.Linq;
using HotChocolate.Configuration;
using Xunit;

namespace HotChocolate.Types
{
    public class SpecedDirectiveTypeTests
    {
        [Fact]
        public void CreateSkipDirective()
        {
            // arrange
            SchemaContext schemaContext = SchemaContextFactory.Create();
            SchemaConfiguration schemaConfiguration =
                new SchemaConfiguration(
                    sp => { },
                    schemaContext.Types,
                    schemaContext.Resolvers,
                    schemaContext.Directives);
            TypeFinalizer typeFinalizer = new TypeFinalizer(schemaConfiguration);
            typeFinalizer.FinalizeTypes(schemaContext, null);

            // assert
            DirectiveType directive = schemaContext.Directives
                .GetDirectiveTypes().FirstOrDefault(t => t.Name == "skip");

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
        public void CreateIncludeDirective()
        {
            // arrange
            SchemaContext schemaContext = SchemaContextFactory.Create();
            SchemaConfiguration schemaConfiguration =
                new SchemaConfiguration(
                        sp => { },
                        schemaContext.Types,
                        schemaContext.Resolvers,
                        schemaContext.Directives);
            TypeFinalizer typeFinalizer = new TypeFinalizer(schemaConfiguration);
            typeFinalizer.FinalizeTypes(schemaContext, null);

            // assert
            DirectiveType directive = schemaContext.Directives
                .GetDirectiveTypes().FirstOrDefault(t => t.Name == "include");

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
