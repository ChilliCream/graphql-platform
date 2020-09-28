using System.Linq;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.ApolloFederation.Directives
{
    public class ProvidesDirectiveTests
        : FederationTypesTestBase
    {
        [Fact]
        public void AddProvidesDirective_EnsureAvailableInSchema()
        {
            // arrange
            ISchema schema = CreateSchema(b =>
            {
                b.AddDirectiveType<ProvidesDirectiveType>();
            });

            // act
            DirectiveType? directive =
                schema.DirectiveTypes.FirstOrDefault(
                    t => t.Name.Equals(TypeNames.Provides));

            // assert
            Assert.NotNull(directive);
            Assert.IsType<ProvidesDirectiveType>(directive);
            Assert.Equal(TypeNames.Provides, directive!.Name);
            Assert.Single(directive.Arguments);
            this.AssertDirectiveHasFieldsArgument(directive);
            Assert.Collection(directive.Locations,
                t => Assert.Equal(DirectiveLocation.FieldDefinition, t));

        }
    }
}
