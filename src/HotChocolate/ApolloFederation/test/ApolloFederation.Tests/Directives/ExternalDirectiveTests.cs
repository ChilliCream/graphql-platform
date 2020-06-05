using System;
using System.Linq;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.ApolloFederation
{
    public class ExternalDirectiveTests
        : FederationTypesTestBase
    {
        [Fact]
        public void AddExternalDirective_EnsureAvailableInSchema()
        {
            // arrange
            ISchema schema = this.CreateSchema(b =>
            {
                b.AddDirectiveType<ExternalDirectiveType>();
            });

            // act
            DirectiveType directive =
                schema.DirectiveTypes.FirstOrDefault(
                    t => t.Name.Equals("external"));

            // assert
            Assert.NotNull(directive);
            Assert.IsType<ExternalDirectiveType>(directive);
            Assert.Equal("external", directive.Name);
            Assert.Equal(0, directive.Arguments.Count());
            Assert.Collection(directive.Locations,
                t => Assert.Equal(DirectiveLocation.FieldDefinition, t));
        }        
    }
}