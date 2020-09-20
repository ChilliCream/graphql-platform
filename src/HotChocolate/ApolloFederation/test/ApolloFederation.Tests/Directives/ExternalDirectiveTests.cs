using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.ApolloFederation.Extensions;
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

        [Fact]
        public void AnnotateExternalToObjectFieldCodeFirst()
        {
            // arrange
            // act
            var schema = Schema.Create(
                t =>
                {
                    t.RegisterQueryType(new ObjectType(
                        o => o.Name("Query")
                            .Field("field")
                            .Argument("a", a => a.Type<StringType>())
                            .Type<StringType>()
                            .External()
                    ));

                    t.RegisterDirective<ExternalDirectiveType>();

                    t.Use(next => context => default(ValueTask));
                });

            ObjectType query = schema.GetType<ObjectType>("Query");
            // assert
            Assert.Collection(query.Fields["field"].Directives,
                item => Assert.Equal("external", item.Name)
            );
        }

        [Fact]
        public void AnnotateExternalToInterfaceFieldSchemaFirst()
        {
            // arrange
            // act
            var schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"
                    type Query {
                        field(a: Int): String
                            @external
                    }

                    interface IQuery {
                        field(a: Int): String
                            @external
                    }
                    ")
                .AddDirectiveType<ExternalDirectiveType>()
                .Use(next => context => default(ValueTask))
                .Create();

            InterfaceType queryInterface = schema.GetType<InterfaceType>("IQuery");
            // assert
            Assert.Collection(queryInterface.Fields["field"].Directives,
                item => Assert.Equal("external", item.Name)
            );
        }
    }
}
