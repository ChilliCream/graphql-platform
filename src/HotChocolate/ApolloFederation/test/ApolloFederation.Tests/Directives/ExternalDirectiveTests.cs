using System.Linq;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.ApolloFederation.Directives
{
    public class ExternalDirectiveTests
        : FederationTypesTestBase
    {
        [Fact]
        public void AddExternalDirective_EnsureAvailableInSchema()
        {
            // arrange
            ISchema schema = CreateSchema(b =>
            {
                b.AddDirectiveType<ExternalDirectiveType>();
            });

            // act
            DirectiveType? directive =
                schema.DirectiveTypes.FirstOrDefault(
                    t => t.Name.Equals(WellKnownTypeNames.External));

            // assert
            Assert.NotNull(directive);
            Assert.IsType<ExternalDirectiveType>(directive);
            Assert.Equal(WellKnownTypeNames.External, directive!.Name);
            Assert.Empty(directive!.Arguments);
            Assert.Collection(directive!.Locations,
                t => Assert.Equal(DirectiveLocation.FieldDefinition, t));
        }

        [Fact]
        public void AnnotateExternalToTypeFieldCodeFirst()
        {
            Snapshot.FullName();

            // arrange
            var schema = Schema.Create(
                t =>
                {
                    t.RegisterQueryType(
                        new ObjectType(
                            o => o.Name("Query")
                                .Field("field")
                                .Argument(
                                    "a",
                                    a => a.Type<StringType>()
                                )
                                .Type<StringType>()
                                .External()
                        )
                    );

                    t.RegisterDirective<ExternalDirectiveType>();
                    t.Use(next => context => default);
                }
            );

            // act
            ObjectType query = schema.GetType<ObjectType>("Query");

            // assert
            Assert.Collection(
                query.Fields["field"].Directives,
                item => Assert.Equal(
                    WellKnownTypeNames.External,
                    item.Name
                )
            );
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void AnnotateExternalToTypeFieldSchemaFirst()
        {
            Snapshot.FullName();

            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"
                    type Query {
                        field(a: Int): String
                            @external
                    }
                    "
                )
                .AddDirectiveType<ExternalDirectiveType>()
                .Use(next => context => default)
                .Create();

            // act
            ObjectType queryInterface = schema.GetType<ObjectType>("Query");

            // assert
            Assert.Collection(
                queryInterface.Fields["field"].Directives,
                item => Assert.Equal(
                    WellKnownTypeNames.External,
                    item.Name
                )
            );
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AnnotateExternalToTypeFieldPureCodeFirst()
        {
            Snapshot.FullName();

            // arrange
            var schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // act
            ObjectType query = schema.GetType<ObjectType>("User");

            // assert
            Assert.Collection(
                query.Fields["idCode"].Directives,
                item => Assert.Equal(
                    WellKnownTypeNames.External,
                    item.Name
                )
            );
            schema.ToString().MatchSnapshot();
        }
    }

    public class Query
    {
        public User GetEntity(int id) => default;
    }

    public class User
    {
        [Key]
        public int Id { get; set; }
        [External]
        public string IdCode { get; set; }
    }
}
