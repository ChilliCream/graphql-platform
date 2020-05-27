using System;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Execution
{
    public class ResolveOperationMiddlewareTests
    {
        [Fact]
        public async Task OperationIsResolved()
        {
            // arrange
            var schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var sourceText = "query a { a }";

            var request = QueryRequestBuilder.New()
                .SetQuery(sourceText)
                .Create();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            );

            context.Document = Utf8GraphQLParser.Parse(sourceText);
            context.QueryKey = "foo";

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.NotNull(context.Operation);
            Assert.Equal("a", context.Operation.Name);
        }

        [Fact]
        public async Task RootValueNotProvidedAndRegisteredWithServices()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.RegisterQueryType<DisposableQuery>();
            });

            var sourceText = "{ isDisposable }";

            var request = QueryRequestBuilder.New()
                .SetQuery(sourceText)
                .Create();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            );

            context.Document = Utf8GraphQLParser.Parse(sourceText);
            context.QueryKey = "foo";

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            DisposableQuery query = Assert.IsType<DisposableQuery>(
                context.Operation.RootValue);
            Assert.True(query.IsDisposed);
        }

        [Fact]
        public async Task RootValueProvidedByRequest()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.RegisterQueryType<DisposableQuery>();
            });

            var rootValue = new DisposableQuery();

            var sourceText = "{ isDisposable }";

            var request = QueryRequestBuilder.New()
                .SetQuery(sourceText)
                .SetInitialValue(rootValue)
                .Create();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            );

            context.Document = Utf8GraphQLParser.Parse(sourceText);
            context.QueryKey = "foo";

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.True(object.ReferenceEquals(
                rootValue, context.Operation.RootValue));
        }

        [Fact]
        public async Task RootValueIsRegisterdAsService()
        {
            // arrange
            var services = new DictionaryServiceProvider(
                typeof(DisposableQuery), new DisposableQuery());

            var schema = Schema.Create(c =>
            {
                c.RegisterQueryType<DisposableQuery>();
            });

            var sourceText = "{ isDisposable }";

            var request = QueryRequestBuilder.New()
                .SetQuery(sourceText)
                .Create();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateRequestServiceScope(services),
                request,
                (f, s) => f.Middleware
            );

            context.Document = Utf8GraphQLParser.Parse(sourceText);
            context.QueryKey = "foo";

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            DisposableQuery query = Assert.IsType<DisposableQuery>(
                context.Operation.RootValue);
            Assert.False(query.IsDisposed);
        }

        [Fact]
        public async Task ProvidedRootValueTakesPrecedenceOverService()
        {
            // arrange
            var services = new DictionaryServiceProvider(
               typeof(DisposableQuery), new DisposableQuery());

            var schema = Schema.Create(c =>
            {
                c.RegisterQueryType<DisposableQuery>();
            });

            var rootValue = new DisposableQuery();

            var sourceText = "{ isDisposable }";

            var request = QueryRequestBuilder.New()
                .SetQuery(sourceText)
                .SetInitialValue(rootValue)
                .Create();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateRequestServiceScope(services),
                request,
                (f, s) => f.Middleware
            );

            context.Document = Utf8GraphQLParser.Parse(sourceText);
            context.QueryKey = "foo";

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.True(object.ReferenceEquals(
                rootValue, context.Operation.RootValue));
        }

        [Fact]
        public async Task RootClrTypeIsObject()
        {
            // arrange
            var schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var sourceText = "query a { a }";

            var request = QueryRequestBuilder.New()
                .SetQuery(sourceText)
                .Create();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            );

            context.Document = Utf8GraphQLParser.Parse(sourceText);
            context.QueryKey = "foo";

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.Null(context.Operation.RootValue);
        }

        [Fact]
        public async Task TwoOperations_ShortHand_QueryException()
        {
            // arrange
            var schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var sourceText = "{ a } query a { a }";

            var request = QueryRequestBuilder.New()
                .SetQuery(sourceText)
                .Create();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            );

            context.Document = Utf8GraphQLParser.Parse(sourceText);
            context.QueryKey = "foo";

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            Func<Task> func = () => middleware.InvokeAsync(context);

            // assert
            QueryException exception =
                await Assert.ThrowsAsync<QueryException>(func);
            Assert.Equal(
                "Only queries that contain one operation can be executed " +
                "without specifying the opartion name.",
                exception.Message);
        }

        [Fact]
        public async Task TwoOperations_WrongOperationName_QueryException()
        {
            // arrange
            var schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var sourceText = "query a { a } query b { a }";

            var request = QueryRequestBuilder.New()
                .SetQuery(sourceText)
                .SetOperation("c")
                .Create();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            );

            context.Document = Utf8GraphQLParser.Parse(sourceText);

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            Func<Task> func = () => middleware.InvokeAsync(context);

            // assert
            QueryException exception =
                await Assert.ThrowsAsync<QueryException>(func);
            Assert.Equal(
                "The specified operation `c` does not exist.",
                exception.Message);
        }

        [InlineData("subscription")]
        [InlineData("mutation")]
        [InlineData("query")]
        [Theory]
        public async Task ResolveRootTypeWithCustomNames(string rootType)
        {
            // arrange
            string queryType = rootType == "query"
                ? string.Empty
                : "type Query { a : String }";

            var schema = Schema.Create(@"
                type Foo { a: String }
                schema { " + rootType + @": Foo }
                " + queryType, c =>
            {
                c.Use(n => ctx => default(ValueTask));
            });

            var sourceText = "query a { a }";

            var request = QueryRequestBuilder.New()
                .SetQuery(sourceText)
                .Create();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            );

            context.QueryKey = "foo";

            context.Document = Utf8GraphQLParser.Parse(sourceText);
            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.NotNull(context.Operation.RootType);
        }

        [Fact]
        public async Task ParseQueryMiddleware_ValidQuery_DocumentIsSet()
        {
            // arrange
            var schema = Schema.Create(@"
                type Query { a(b:String): String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var sourceText = "query foo($a: String) { a }";

            var request = QueryRequestBuilder.New()
                .SetQuery(sourceText)
                .SetVariableValue("a", "abc")
                .Create();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            );
            context.QueryKey = "Foo";

            context.Document = Utf8GraphQLParser.Parse(sourceText);

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.NotNull(context.Operation.Variables);
            Assert.Equal(
                "abc",
                context.Operation.Variables.GetVariable<string>("a"));
        }

        public class DisposableQuery
            : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}
