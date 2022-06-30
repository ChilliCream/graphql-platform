using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.ApolloFederation;

public class FederationSchemaPrinterTests
{
    [Fact]
    public void TestFederationPrinter_ShouldThrow()
    {
        // arrange
        ISchema? schema = null;
        void Action() => FederationSchemaPrinter.Print(schema!);

        // act
        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void TestFederationPrinterApolloDirectivesSchemaFirst()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddDocumentFromString(
                @"type TestType @key(fields: ""id"") {
                    id: Int!
                    name: String!
                }

                type Query {
                    someField(a: Int): TestType
                }")
            .Use(_ => _ => default)
            .Create();

        // act
        // assert
        FederationSchemaPrinter.Print(schema).MatchSnapshot();
    }

    [Fact]
    public void TestFederationPrinterSchemaFirst()
    {
        // arrange
        var schema = SchemaBuilder.New()
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
            .Use(_ => _ => default)
            .Create();

        // act
        // assert
        FederationSchemaPrinter.Print(schema).MatchSnapshot();
    }

    [Fact]
    public void TestFederationPrinterSchemaFirst_With_DateTime()
    {
        // arrange
        var schema = SchemaBuilder.New()
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
                  time: DateTime
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

                scalar DateTime
            ")
            .Use(_ => _ => default)
            .Create();

        // act
        // assert
        FederationSchemaPrinter.Print(schema).MatchSnapshot();
    }

    [Fact]
    public void TestFederationPrinterApolloDirectivesPureCodeFirst()
    {
        // arrange
        var schema = SchemaBuilder.New()
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
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<QueryRoot<Product>>()
            .Create();

        // act
        // assert
        FederationSchemaPrinter.Print(schema).MatchSnapshot();
    }

    [Fact]
    public void CustomDirective_IsPublic()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryWithDirective>()
            .AddDirectiveType(new CustomDirectiveType(true))
            .Create();

        // act
        // assert
        FederationSchemaPrinter.Print(schema).MatchSnapshot();
    }

    [Fact]
    public void CustomDirective_IsInternal()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryWithDirective>()
            .AddDirectiveType(new CustomDirectiveType(false))
            .Create();

        // act
        // assert
        FederationSchemaPrinter.Print(schema).MatchSnapshot();
    }

    public class QueryRoot<T>
    {
        public T GetEntity(int id) => default!;
    }

    public class User
    {
        [Key]
        public int Id { get; set; }
        [External]
        public string IdCode { get; set; } = default!;
        [Requires("idCode")]
        public string IdCodeShort { get; set; } = default!;
        [Provides("zipcode")]
        public Address Address { get; set; } = default!;
    }

    public class Address
    {
        [External]
        public string Zipcode { get; set; } = default!;
    }

    [ExtendServiceType]
    public class Product
    {
        [Key]
        public string Upc { get; set; } = default!;
    }

    public class QueryWithDirective : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Name("Query")
                .Field("foo")
                .Resolve("bar")
                .Directive("custom");

            descriptor
                .Field("deprecated1")
                .Resolve("abc")
                .Deprecated("deprecated")
                .Type<EnumType<EnumWithDeprecatedValue>>();

            descriptor
                .Field("deprecated2")
                .Resolve("abc")
                .Deprecated("deprecated")
                .Directive("custom")
                .Type<EnumType<EnumWithDeprecatedValue>>();
        }
    }

    public class CustomDirectiveType : DirectiveType
    {
        private readonly bool _isPublic;

        public CustomDirectiveType(bool isPublic)
        {
            _isPublic = isPublic;
        }

        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name("custom")
                .Location(DirectiveLocation.FieldDefinition)
                .Location(DirectiveLocation.EnumValue);

            if (_isPublic)
            {
                descriptor.Public();
            }
            else
            {
                descriptor.Internal();
            }
        }
    }

    public enum EnumWithDeprecatedValue
    {
        [Obsolete]
        Deprecated1,

        [CustomDirective]
        [Obsolete]
        Deprecated2,

        Active
    }

    public class CustomDirectiveAttribute : DescriptorAttribute
    {
        protected override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is EnumValueDescriptor enumValue)
            {
                enumValue.Directive("custom");
            }
        }
    }
}
