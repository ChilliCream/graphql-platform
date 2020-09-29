using System.Linq;
using HotChocolate.ApolloFederation.Extensions;
using HotChocolate.Types;
using Snapshooter.Xunit;
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
                [Fact]
        public void AnnotateProvidesToFieldSchemaFirst()
        {
            Snapshot.FullName();

            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"
                    type Review @key(fields: ""id"") {
                        id: Int!
                        product: Product! @requires(fields: ""id"")
                    }

                    type Product {
                        name: String!
                    }

                    type Query {
                        someField(a: Int): Review
                    }
                ")
                .AddDirectiveType<KeyDirectiveType>()
                .AddDirectiveType<RequiresDirectiveType>()
                .AddType<FieldSetType>()
                .Use(next => context => default)
                .Create();

            // act
            ObjectType testType = schema.GetType<ObjectType>("Review");

            // assert
            Assert.Collection(testType.Fields.Single(field => field.Name.Value == "product").Directives,
                providesDirective =>
                {
                    Assert.Equal(
                        TypeNames.Requires,
                        providesDirective.Name
                    );
                    Assert.Equal("fields", providesDirective.ToNode().Arguments[0].Name.ToString());
                    Assert.Equal("\"id\"", providesDirective.ToNode().Arguments[0].Value.ToString());
                }
            );
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AnnotateProvidesToFieldCodeFirst()
        {
            Snapshot.FullName();

            // arrange
            var schema = Schema.Create(
                t =>
                {
                    var productType = new ObjectType(
                        o =>
                        {
                            o.Name("Product");
                            o.Field("name")
                                .Type<StringType>();
                        }
                    );

                    var reviewType = new ObjectType(
                        o =>
                        {
                            o.Name("Review")
                                .Key("id");
                            o.Field("id")
                                .Type<IntType>();
                            o.Field("product")
                                .Requires("id")
                                .Type(productType);
                        }
                    );
                    t.RegisterType(reviewType);
                    t.RegisterQueryType(new ObjectType(
                        o => o.Name("Query")
                            .Field("someField")
                            .Argument("a", a => a.Type<IntType>())
                            .Type(reviewType)
                    ));

                    t.RegisterDirective<KeyDirectiveType>();
                    t.RegisterDirective<RequiresDirectiveType>();
                    t.RegisterType<FieldSetType>();
                    t.Use(next => context => default);
                });

            // act
            ObjectType testType = schema.GetType<ObjectType>("Review");

            // assert
            Assert.Collection(testType.Fields.Single(field => field.Name.Value == "product").Directives,
                providesDirective =>
                {
                    Assert.Equal(
                        TypeNames.Requires,
                        providesDirective.Name
                    );
                    Assert.Equal("fields", providesDirective.ToNode().Arguments[0].Name.ToString());
                    Assert.Equal("\"id\"", providesDirective.ToNode().Arguments[0].Value.ToString());
                }
            );
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AnnotateProvidesToClassAttributePureCodeFirst()
        {
            Snapshot.FullName();

            // arrange
            var schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // act
            ObjectType testType = schema.GetType<ObjectType>("Review");

            // assert
            Assert.Collection(testType.Fields.Single(field => field.Name.Value == "product").Directives,
                providesDirective =>
                {
                    Assert.Equal(
                        TypeNames.Requires,
                        providesDirective.Name
                    );
                    Assert.Equal("fields", providesDirective.ToNode().Arguments[0].Name.ToString());
                    Assert.Equal("\"id\"", providesDirective.ToNode().Arguments[0].Value.ToString());
                }
            );
            schema.ToString().MatchSnapshot();
        }

        public class Query
        {
            public Review someField(int id) => default;
        }

        public class Review
        {
            [Key]
            public int Id { get; set; }
            [Requires("id")]
            public Product Product { get; set; }
        }

        public class Product
        {
            public string Name { get; set; }
        }
    }
}
