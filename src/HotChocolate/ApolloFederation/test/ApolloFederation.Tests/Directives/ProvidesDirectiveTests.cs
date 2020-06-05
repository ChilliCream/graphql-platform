using System.Linq;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.ApolloFederation
{
    public class ProvidesDirectiveTests
        : FederationTypesTestBase
    {
        [Fact]
        public void AddProvidesDirective_EnsureAvailableInSchema()
        {
            // arrange
            ISchema schema = this.CreateSchema(b =>
            {
                b.AddDirectiveType<ProvidesDirectiveType>();
            });

            // act
            DirectiveType directive =
                schema.DirectiveTypes.FirstOrDefault(
                    t => t.Name.Equals("provides"));

            // assert
            Assert.NotNull(directive);
            Assert.IsType<ProvidesDirectiveType>(directive);
            Assert.Equal("provides", directive.Name);
            Assert.Equal(1, directive.Arguments.Count());
            this.AssertDirectiveHasFieldsArgument(directive);
            Assert.Collection(directive.Locations,
                t => Assert.Equal(DirectiveLocation.FieldDefinition, t));

        }
    }
}