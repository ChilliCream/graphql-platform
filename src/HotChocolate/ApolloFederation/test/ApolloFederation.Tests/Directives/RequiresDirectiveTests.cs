using System.Linq;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.ApolloFederation.Directives
{
    public class RequiresDirectiveTests
        : FederationTypesTestBase
    {
        [Fact]
        public void AddRequiresDirective_EnsureAvailableInSchema()
        {
            // arrange
            ISchema schema = CreateSchema(b =>
            {
                b.AddDirectiveType<RequiresDirectiveType>();
            });

            // act
            DirectiveType? directive =
                schema.DirectiveTypes.FirstOrDefault(
                    t => t.Name.Equals(TypeNames.Requires));

            // assert
            Assert.NotNull(directive);
            Assert.IsType<RequiresDirectiveType>(directive);
            Assert.Equal(TypeNames.Requires, directive!.Name);
            Assert.Single(directive.Arguments);
            this.AssertDirectiveHasFieldsArgument(directive);
            Assert.Collection(directive.Locations,
                t => Assert.Equal(DirectiveLocation.FieldDefinition, t));

        }
    }
}
