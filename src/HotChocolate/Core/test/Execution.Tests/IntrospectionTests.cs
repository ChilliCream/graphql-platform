using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ChilliCream.Testing;
using HotChocolate.Configuration;
using HotChocolate.Tests;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;
using Snapshot = Snapshooter.Xunit.Snapshot;

namespace HotChocolate.Execution;

public class IntrospectionTests
{
    [Fact]
    public async Task TypeNameIntrospectionOnQuery()
    {
        // arrange
        var query = "{ __typename }";
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task TypeNameIntrospectionNotOnQuery()
    {
        // arrange
        var query = "{ b { __typename } }";
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Query_Specified_By()
    {
        // arrange
        var query = "{ __type (name: \"DateTime\") { specifiedByURL } }";

        var executor =
            SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<DateTimeType>()
                    .Resolve(default(DateTime)))
                .Create()
                .MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task TypeIntrospectionOnQuery()
    {
        // arrange
        var query = "{ __type (name: \"Foo\") { name } }";
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task TypeIntrospectionOnQueryWithFields()
    {
        // arrange
        var query =
            "{ __type (name: \"Foo\") " +
            "{ name fields { name type { name } } } }";
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteGraphiQLIntrospectionQuery()
    {
        // arrange
        var query = FileResource.Open("IntrospectionQuery.graphql");
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteGraphiQLIntrospectionQuery_ToJson()
    {
        // arrange
        var query = FileResource.Open("IntrospectionQuery.graphql");
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task FieldMiddlewareDoesNotHaveAnEffectOnIntrospection()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .Use(next => async context =>
            {
                await next.Invoke(context);

                if (context.Result is string s)
                {
                    context.Result = s.ToUpperInvariant();
                }
            })
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ __typename a }");

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task FieldMiddlewareHasAnEffectOnIntrospectIfSwitchedOn()
    {
        // arrange
        var query = "{ __typename a }";

        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .Use(next => async context =>
            {
                await next.Invoke(context);

                if (context.Result is string s)
                {
                    context.Result = s.ToUpperInvariant();
                }
            })
            .ModifyOptions(o =>
                o.FieldMiddleware = FieldMiddlewareApplication.AllFields)
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task DirectiveMiddlewareDoesWorkOnIntrospection()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddDirectiveType<UpperDirectiveType>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ __typename @upper a }");

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task DefaultValueIsInputObject()
    {
        // arrange
        var query = FileResource.Open("IntrospectionQuery.graphql");
        var executor = SchemaBuilder.New()
            .AddQueryType<BarType>()
            .ModifyOptions(o => o.RemoveUnreachableTypes = false)
            .Create()
            .MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task DirectiveIntrospection_AllDirectives_Public()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                @"
                        type Query {
                            foo: String
                                @foo
                                @bar(baz: ""ABC"")
                                @bar(baz: null)
                                @bar(quox: { a: ""ABC"" })
                                @bar(quox: { })
                                @bar
                        }

                        input SomeInput {
                            a: String!
                        }

                        directive @foo on FIELD_DEFINITION

                        directive @bar(baz: String quox: SomeInput) repeatable on FIELD_DEFINITION
                    ")
            .UseField(next => next)
            .ModifyOptions(o => o.EnableDirectiveIntrospection = true)
            .ExecuteRequestAsync(
                @"
                        {
                            __schema {
                                types {
                                    fields {
                                        appliedDirectives {
                                            name
                                            args {
                                                name
                                                value
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    ")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DirectiveIntrospection_AllDirectives_Internal()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                @"
                        type Query {
                            foo: String
                                @foo
                                @bar(baz: ""ABC"")
                                @bar(baz: null)
                                @bar(quox: { a: ""ABC"" })
                                @bar(quox: { })
                                @bar
                        }

                        input SomeInput {
                            a: String!
                        }

                        directive @foo on FIELD_DEFINITION

                        directive @bar(baz: String quox: SomeInput) repeatable on FIELD_DEFINITION
                    ")
            .UseField(next => next)
            .ModifyOptions(o => o.EnableDirectiveIntrospection = true)
            .ModifyOptions(o => o.DefaultDirectiveVisibility = DirectiveVisibility.Internal)
            .ExecuteRequestAsync(
                @"
                        {
                            __schema {
                                types {
                                    fields {
                                        appliedDirectives {
                                            name
                                            args {
                                                name
                                                value
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    ")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DirectiveIntrospection_SomeDirectives_Internal()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                @"
                        type Query {
                            foo: String
                                @foo
                                @bar(baz: ""ABC"")
                                @bar(baz: null)
                                @bar(quox: { a: ""ABC"" })
                                @bar(quox: { })
                                @bar
                        }

                        input SomeInput {
                            a: String!
                        }

                        directive @bar(baz: String quox: SomeInput) repeatable on FIELD_DEFINITION
                    ")
            .UseField(next => next)
            .ModifyOptions(o => o.EnableDirectiveIntrospection = true)
            .AddDirectiveType(new DirectiveType(d =>
            {
                d.Name("foo");
                d.Location(DirectiveLocation.FieldDefinition);
                d.Internal();
            }))
            .ExecuteRequestAsync(
                @"
                        {
                            __schema {
                                types {
                                    fields {
                                        appliedDirectives {
                                            name
                                            args {
                                                name
                                                value
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    ")
            .MatchSnapshotAsync();
    }

    private static ISchema CreateSchema()
    {
        return SchemaBuilder.New()
            .AddType<BarDirectiveType>()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.RemoveUnreachableTypes = false)
            .Create();
    }

    private sealed class Query : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");

            descriptor.Field("a")
                .Type<StringType>()
                .Resolve(() => "a");

            descriptor.Field("b")
                .Type<Foo>()
                .Resolve(() => new object());

            descriptor.Field("c")
                .Type<StringType>()
                .Argument("c_arg", x => x.Type<StringType>().Deprecated("TEST"))
                .Resolve(() => "c");

            descriptor.Field("d")
                .Type<StringType>()
                .Argument("d_arg", x => x.Type<FooInput>())
                .Resolve(() => "d");
        }
    }

    private sealed class BarDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("Bar");
            descriptor.Location(DirectiveLocation.Query | DirectiveLocation.Field);
            descriptor.Argument("a").Type<StringType>();
            descriptor.Argument("b").Type<StringType>().Deprecated("TEST 3");
        }
    }

    private sealed class FooInput : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("FooInput");

            descriptor.Field("a").Type<StringType>();

            descriptor.Field("b").Type<StringType>().Deprecated("TEST 2");
        }
    }

    private sealed class Foo : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");

            descriptor.Field("a")
                .Type<StringType>()
                .Resolve(() => "foo.a");
        }
    }

    private sealed class BarType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Bar");
            descriptor.Field("a")
                .Type<StringType>()
                .Argument("b",
                    a => a.Type<BazType>()
                        .DefaultValue(new Baz { Qux = "fooBar" }))
                .Resolve(() => "foo.a");
        }
    }

    public class BazType : InputObjectType<Baz>
    {
        protected override void Configure(IInputObjectTypeDescriptor<Baz> descriptor)
        {
            descriptor.Name("Baz");
            descriptor.Field(t => t.Qux).DefaultValue("123456");
        }
    }

    public class Baz
    {
        public string Qux { get; set; }
    }

    private sealed class UpperDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("upper");
            descriptor.Location(DirectiveLocation.Field);
            descriptor.Use(next => async context =>
            {
                await next.Invoke(context);

                if (context.Result is string s)
                {
                    context.Result = s.ToUpperInvariant();
                }
            });
        }
    }
}