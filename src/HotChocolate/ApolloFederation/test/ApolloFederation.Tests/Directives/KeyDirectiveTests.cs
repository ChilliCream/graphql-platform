using System.Linq;
using HotChocolate.ApolloFederation.Extensions;
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
            ISchema schema = CreateSchema(b =>
            {
                b.AddDirectiveType<KeyDirectiveType>();
            });

            // act
            DirectiveType? directive =
                schema.DirectiveTypes.FirstOrDefault(
                    t => t.Name.Equals(TypeNames.Key));

            // assert
            Assert.NotNull(directive);
            Assert.IsType<KeyDirectiveType>(directive);
            Assert.Equal(TypeNames.Key, directive!.Name);
            Assert.Single(directive.Arguments);
            AssertDirectiveHasFieldsArgument(directive);
            Assert.Collection(directive.Locations,
                t => Assert.Equal(DirectiveLocation.Object, t),
                t => Assert.Equal(DirectiveLocation.Interface, t));

        }

        [Fact]
        public void AnnotateKeyToObjectTypeCodeFirst()
        {
            // arrange
            var schema = Schema.Create(
                t =>
                {
                    var testTypeDefinition = new ObjectType(
                        o =>
                        {
                            o.Name("TestType")
                                .Key("id");
                            o.Field("id")
                                .Type<IntType>();
                            o.Field("name")
                                .Type<StringType>();
                        }
                    );
                    t.RegisterType(testTypeDefinition);
                    t.RegisterQueryType(new ObjectType(
                        o => o.Name("Query")
                            .Field("someField")
                            .Argument("a", a => a.Type<IntType>())
                            .Type(testTypeDefinition)
                    ));

                    t.RegisterDirective<KeyDirectiveType>();
                    t.RegisterType<FieldSetType>();
                    t.Use(next => context => default);
                });

            // act
            ObjectType testType = schema.GetType<ObjectType>("TestType");
            // assert
            Assert.Collection(testType.Directives,
                item =>
                {
                    Assert.Equal(
                        TypeNames.Key,
                        item.Name
                    );
                    Assert.Equal("fields", item.ToNode().Arguments[0].Name.ToString());
                    Assert.Equal("\"id\"", item.ToNode().Arguments[0].Value.ToString());
                }
            );
        }

        [Fact]
        public void AnnotateKeyToObjectTypeSchemaFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"
                    type TestType @key(fields: ""id"") {
                        id: Int!
                        name: String!
                    }

                    type Query {
                        someField(a: Int): TestType
                    }

                    interface IQuery {
                        someField(a: Int): TestType
                    }")
                .AddDirectiveType<KeyDirectiveType>()
                .AddType<FieldSetType>()
                .Use(next => context => default)
                .Create();

            // act
            ObjectType testType = schema.GetType<ObjectType>("TestType");

            // assert
            Assert.Collection(testType.Directives,
                item =>
                {
                    Assert.Equal(
                        TypeNames.Key,
                        item.Name
                    );
                    Assert.Equal("fields", item.ToNode().Arguments[0].Name.ToString());
                    Assert.Equal("\"id\"", item.ToNode().Arguments[0].Value.ToString());
                }
            );
        }

        [Fact]
        public void AnnotateKeyToObjectTypePureCodeFirst()
        {
            // arrange
            var schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query>()
                .Create();

            // act
            ObjectType testType = schema.GetType<ObjectType>("TestType");

            // assert
            Assert.Collection(testType.Directives,
                item =>
                {
                    Assert.Equal(
                        TypeNames.Key,
                        item.Name
                    );
                    Assert.Equal("fields", item.ToNode().Arguments[0].Name.ToString());
                    Assert.Equal("\"id\"", item.ToNode().Arguments[0].Value.ToString());
                }
            );
        }

        public class Query
        {
            public TestType someField(int id)
            {
                return new TestType();
            }
        }

        [Key("id")]
        public class TestType
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
