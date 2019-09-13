using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Configuration;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class IntrospectionTests
    {
        [Fact]
        public async Task TypeNameIntrospectionOnQuery()
        {
            // arrange
            string query = "{ __typename }";
            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task TypeNameIntrospectionNotOnQuery()
        {
            // arrange
            string query = "{ b { __typename } }";
            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task TypeIntrospectionOnQuery()
        {
            // arrange
            string query = "{ __type (name: \"Foo\") { name } }";
            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task TypeIntrospectionOnQueryWithFields()
        {
            // arrange
            string query =
                "{ __type (name: \"Foo\") " +
                "{ name fields { name type { name } } } }";
            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteGraphiQLIntrospectionQuery()
        {
            // arrange
            string query =
                FileResource.Open("IntrospectionQuery.graphql");
            IQueryExecutor executor = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task FieldMiddlewareDoesNotHaveAnEffectOnIntrospection()
        {
            // arrange
            string query = "{ __typename a }";

            var schema = Schema.Create(c =>
            {
                c.RegisterExtendedScalarTypes();
                c.RegisterType<Query>();
                c.Use(next => async context =>
                {
                    await next.Invoke(context);

                    if (context.Result is string s)
                    {
                        context.Result = s.ToUpperInvariant();
                    }
                });
            });

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task FieldMiddlewareHasAnEffectOnIntrospectIfSwitchedOn()
        {
            // arrange
            string query = "{ __typename a }";

            ISchema schema = SchemaBuilder.New()
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

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task DirectiveMiddlewareDoesWorkOnIntrospection()
        {
            // arrange
            string query = "{ __typename @upper a }";

            var schema = Schema.Create(c =>
            {
                c.RegisterExtendedScalarTypes();
                c.RegisterType<Query>();
                c.RegisterDirective<UpperDirectiveType>();
            });

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task DefaultValueIsInputObject()
        {
            // arrange
            string query = FileResource.Open("IntrospectionQuery.graphql");
            IQueryExecutor executor = Schema.Create(t =>
                t.RegisterQueryType<BarType>())
                .MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        private static Schema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterExtendedScalarTypes();
                c.RegisterType<Query>();
            });
        }

        private class Query
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");

                descriptor.Field("a")
                    .Type<StringType>()
                    .Resolver(() => "a");

                descriptor.Field("b")
                    .Type<Foo>()
                    .Resolver(() => new object());
            }
        }

        private class Foo
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Foo");

                descriptor.Field("a")
                    .Type<StringType>()
                    .Resolver(() => "foo.a");
            }
        }

        private class BarType
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Bar");
                descriptor.Field("a")
                    .Type<StringType>()
                    .Argument("b", a => a.Type<BazType>()
                        .DefaultValue(new Baz { Qux = "fooBar" }))
                    .Resolver(() => "foo.a");
            }
        }

        public class BazType
            : InputObjectType<Baz>
        {
            protected override void Configure(
                IInputObjectTypeDescriptor<Baz> descriptor)
            {
                descriptor.Name("Baz");
                descriptor.Field(t => t.Qux).DefaultValue("123456");
            }
        }


        public class Baz
        {
            public string Qux { get; set; }
        }

        private sealed class UpperDirectiveType
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
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
}
