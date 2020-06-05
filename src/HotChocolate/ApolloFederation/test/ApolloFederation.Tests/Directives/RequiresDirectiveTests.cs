using System.Linq;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.ApolloFederation
{
    public class RequiresDirectiveTests
        : FederationTypesTestBase
    {
        [Fact]
        public void AddRequiresDirective_EnsureAvailableInSchema()
        {
            // arrange
            ISchema schema = this.CreateSchema(b =>
            {
                b.AddDirectiveType<RequiresDirectiveType>();
            });

            // act
            DirectiveType directive =
                schema.DirectiveTypes.FirstOrDefault(
                    t => t.Name.Equals("requires"));

            // assert
            Assert.NotNull(directive);
            Assert.IsType<RequiresDirectiveType>(directive);
            Assert.Equal("requires", directive.Name);
            Assert.Equal(1, directive.Arguments.Count());
            Assert.Collection(directive.Arguments,
                t =>
                {
                    Assert.Equal("fields", t.Name);
                    Assert.IsType<FieldSetType>(
                        Assert.IsType<NonNullType>(t.Type).Type);
                }
            );
            Assert.Collection(directive.Locations,
                t => Assert.Equal(DirectiveLocation.FieldDefinition, t));

        }
    }
}