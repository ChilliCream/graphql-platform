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
            Schema schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var request = new QueryRequest("query a { a }").ToReadOnly();

            var context = new QueryContext(
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request);

            context.Document = Parser.Default.Parse(request.Query);

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
            Schema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<DisposableQuery>();
            });

            var request = new QueryRequest("{ isDisposable }").ToReadOnly();

            var context = new QueryContext(
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request);

            context.Document = Parser.Default.Parse(request.Query);

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
            Schema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<DisposableQuery>();
            });

            var rootValue = new DisposableQuery();

            var request = new QueryRequest("{ isDisposable }")
            {
                InitialValue = rootValue
            }.ToReadOnly();

            var context = new QueryContext(
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request);

            context.Document = Parser.Default.Parse(request.Query);

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.True(object.ReferenceEquals(
                rootValue, context.Operation.RootValue));
        }

        [Fact]
        public async Task RooValueIsRegisterdAsService()
        {
            // arrange
            var services = new DictionaryServiceProvider(
                typeof(DisposableQuery), new DisposableQuery());

            Schema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<DisposableQuery>();
            });

            var request = new QueryRequest("{ isDisposable }").ToReadOnly();

            var context = new QueryContext(
                schema,
                services.CreateRequestServiceScope(),
                request);

            context.Document = Parser.Default.Parse(request.Query);

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

            Schema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<DisposableQuery>();
            });

            var rootValue = new DisposableQuery();

            var request = new QueryRequest("{ isDisposable }")
            {
                InitialValue = rootValue
            }.ToReadOnly();

            var context = new QueryContext(
                schema,
                services.CreateRequestServiceScope(),
                request);

            context.Document = Parser.Default.Parse(request.Query);

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
            Schema schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var request = new QueryRequest("query a { a }").ToReadOnly();

            var context = new QueryContext(
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request);

            context.Document = Parser.Default.Parse(request.Query);

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
            Schema schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var request = new QueryRequest("{ a } query a { a }").ToReadOnly();

            var context = new QueryContext(
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request);

            context.Document = Parser.Default.Parse(request.Query);

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
            Schema schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var request = new QueryRequest(
                "query a { a } query b { a }", "c")
                .ToReadOnly();

            var context = new QueryContext(
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request);

            context.Document = Parser.Default.Parse(request.Query);

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
            Schema schema = Schema.Create(@"
                type Foo { a: String }
                schema { " + rootType + @": Foo }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Foo", "a");
            });

            var request = new QueryRequest("query a { a }").ToReadOnly();

            var context = new QueryContext(
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request);

            context.Document = Parser.Default.Parse(request.Query);

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.NotNull(context.Operation.RootType);
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
