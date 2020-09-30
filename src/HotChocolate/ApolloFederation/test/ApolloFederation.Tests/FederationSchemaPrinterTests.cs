using System;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.ApolloFederation
{
    public class FederationSchemaPrinterTests
    {
        [Fact]
        public void TestFederationPrinter_ShouldThrow()
        {
            // arrange
            ISchema? schema = null;
            void action() => FederationSchemaPrinter.Print(schema);

            // act
            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void TestFederationPrinterApolloDirectivesSchemaFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddDocumentFromString(@"
                    type TestType @key(fields: ""id"") {
                        id: Int!
                        name: String!
                    }

                    type Query {
                        someField(a: Int): TestType
                    }
                ")
                .Use(next => context => default)
                .Create();

            // act
            // assert
            FederationSchemaPrinter.Print(schema).MatchSnapshot();
        }

        [Fact]
        public void TestFederationPrinterSchemaFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddDocumentFromString(@"
                    type TestType @key(fields: ""id"") {
                        id: Int!
                        name: String!
                        enum: SomeEnum
                    }

                    type TestTypeTwo {
                        id: Int!
                    }

                    interface iTestType @key(fields: ""id"") {
                        id: Int!
                        external: String! @external
                    }

                    union TestTypes = TestType | TestTypeTwo

                    enum SomeEnum {
                       FOO
                       BAR
                    }

                    input SomeInput {
                      name: String!
                      description: String
                      someEnum: SomeEnum
                    }

                    type Mutation {
                        doSomething(input: SomeInput): Boolean
                    }

                    type Query implements iQuery {
                        someField(a: Int): TestType
                    }

                    interface iQuery {
                        someField(a: Int): TestType
                    }
                ")
                .Use(next => context => default)
                .Create();

            // act
            // assert
            FederationSchemaPrinter.Print(schema).MatchSnapshot();
        }

        [Fact(Skip = "Wait for SchemaFirstFixes!")]
        public void TestFederationPrinterApolloTypeExtensionSchemaFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddDocumentFromString(
                    @"
                extend type TestType @key(fields: ""id"") {
                    id: Int!
                    name: String!
                }

                type Query {
                    someField(a: Int): TestType
                }")
                .Use(next => context => default)
                .Create();

            // act
            // assert
            FederationSchemaPrinter.Print(schema).MatchSnapshot();
        }

        [Fact]
        public void TestFederationPrinterApolloDirectivesPureCodeFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<QueryRoot<User>>()
                .Create();

            // act
            // assert
            FederationSchemaPrinter.Print(schema).MatchSnapshot();
        }

        [Fact]
        public void TestFederationPrinterTypeExtensionPureCodeFirst()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<QueryRoot<Product>>()
                .Create();

            // act
            // assert
            FederationSchemaPrinter.Print(schema).MatchSnapshot();
        }

        public class QueryRoot<T>
        {
            public T GetEntity(int id) => default;
        }

        public class User
        {
            [Key]
            public int Id { get; set; }
            [External]
            public string IdCode { get; set; }
            [Requires("idCode")]
            public string IdCodeShort { get; set; }
            [Provides("zipcode")]
            public Address Address { get; set; }
        }

        public class Address
        {
            [External]
            public string Zipcode { get; set; }
        }

        [ForeignServiceTypeExtension]
        public class Product
        {
            [Key]
            public string Upc { get; set; }
        }
    }
}
