using System.Linq;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.ApolloFederation
{
    public class KeyDirectiveTests
        : FederationTypesTestBase
    {
        [Fact]
        public void AddKeyDirective_EnsureAvailableInSchema()
        {
            // arrange
            ISchema schema = this.CreateSchema(b =>
            {
                b.AddDirectiveType<KeyDirectiveType>();
            });

            // act
            DirectiveType directive =
                schema.DirectiveTypes.FirstOrDefault(
                    t => t.Name.Equals("key"));

            // assert
            Assert.NotNull(directive);
            Assert.IsType<KeyDirectiveType>(directive);
            Assert.Equal("key", directive.Name);
            Assert.Equal(1, directive.Arguments.Count());
            this.AssertDirectiveHasFieldsArgument(directive);
            Assert.Collection(directive.Locations,
                t => Assert.Equal(DirectiveLocation.Object, t),
                t => Assert.Equal(DirectiveLocation.Interface, t));

        }
    }
}