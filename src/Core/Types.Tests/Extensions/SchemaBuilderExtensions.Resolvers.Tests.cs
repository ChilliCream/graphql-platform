using System;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    public class SchemaBuilderExtensionsResolversTests
    {
        [Fact]
        public void AddResolverContextObject_BuilderIsNull_ArgNullExcept()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddResolver(
                    null,
                    "A",
                    "B",
                    new Func<IResolverContext, object>(c => new object()));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }


        [Fact]
        public void AddResolverContextObject_ResolverIsNull_ArgNullExcept()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddResolver(
                    builder,
                    "A",
                    "B",
                    (Func<IResolverContext, object>)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddResolverContextObject_ResolveField()
        {
            // arrange
            var builder = new SchemaBuilder();
            builder.AddDocumentFromString("type Query { foo: String }");

            // act
            SchemaBuilderExtensions
                .AddResolver(
                    builder,
                    "Query",
                    "foo",
                    new Func<IResolverContext, object>(c => "bar"));

            // assert
            builder.Create()
                .MakeExecutable()
                .ExecuteAsync("{ foo }")
                .Result
                .MatchSnapshot();
        }

        [Fact]
        public void AddResolverContextTaskObject_BuilderIsNull_ArgNullExcept()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddResolver(
                    null,
                    "A",
                    "B",
                    new Func<IResolverContext, Task<object>>(
                        c => Task.FromResult(new object())));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }


        [Fact]
        public void AddResolverContextTaskObject_ResolverIsNull_ArgNullExcept()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddResolver(
                    builder,
                    "A",
                    "B",
                    (Func<IResolverContext, Task<object>>)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddResolverContextTaskObject_ResolveField()
        {
            // arrange
            var builder = new SchemaBuilder();
            builder.AddDocumentFromString("type Query { foo: String }");

            // act
            SchemaBuilderExtensions
                .AddResolver(
                    builder,
                    "Query",
                    "foo",
                    new Func<IResolverContext, Task<object>>(
                        c => Task.FromResult<object>("bar")));

            // assert
            builder.Create()
                .MakeExecutable()
                .ExecuteAsync("{ foo }")
                .Result
                .MatchSnapshot();
        }

        [Fact]
        public void AddResolverContextTResult_BuilderIsNull_ArgNullExcept()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddResolver(
                    null,
                    "A",
                    "B",
                    new Func<IResolverContext, string>(
                        c => "abc"));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }


        [Fact]
        public void AddResolverContextTResult_ResolverIsNull_ArgNullExcept()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddResolver(
                    builder,
                    "A",
                    "B",
                    (Func<IResolverContext, string>)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddResolverContextTResult_ResolveField()
        {
            // arrange
            var builder = new SchemaBuilder();
            builder.AddDocumentFromString("type Query { foo: String }");

            // act
            SchemaBuilderExtensions
                .AddResolver(
                    builder,
                    "Query",
                    "foo",
                    new Func<IResolverContext, string>(
                        c => "bar"));

            // assert
            builder.Create()
                .MakeExecutable()
                .ExecuteAsync("{ foo }")
                .Result
                .MatchSnapshot();
        }

        [Fact]
        public void AddResolverContextTaskTResult_BuilderIsNull_ArgNullExcept()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddResolver(
                    null,
                    "A",
                    "B",
                    new Func<IResolverContext, Task<string>>(
                        c => Task.FromResult("abc")));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }


        [Fact]
        public void AddResolverContextTaskTResult_ResolverIsNull_ArgNullExcept()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddResolver(
                    builder,
                    "A",
                    "B",
                    (Func<IResolverContext, Task<string>>)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddResolverContextTaskTResult_ResolveField()
        {
            // arrange
            var builder = new SchemaBuilder();
            builder.AddDocumentFromString("type Query { foo: String }");

            // act
            SchemaBuilderExtensions
                .AddResolver(
                    builder,
                    "Query",
                    "foo",
                    new Func<IResolverContext, Task<string>>(
                        c => Task.FromResult("bar")));

            // assert
            builder.Create()
                .MakeExecutable()
                .ExecuteAsync("{ foo }")
                .Result
                .MatchSnapshot();
        }

        [Fact]
        public void AddResolverObject_BuilderIsNull_ArgNullExcept()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddResolver(
                    null,
                    "A",
                    "B",
                    new Func<object>(() => "abc"));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }


        [Fact]
        public void AddResolverObject_ResolverIsNull_ArgNullExcept()
        {
            // arrange
            var builder = new SchemaBuilder();

            // act
            Action action = () => SchemaBuilderExtensions
                .AddResolver(
                    builder,
                    "A",
                    "B",
                    (Func<object>)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddResolverObject_ResolveField()
        {
            // arrange
            var builder = new SchemaBuilder();
            builder.AddDocumentFromString("type Query { foo: String }");

            // act
            SchemaBuilderExtensions
                .AddResolver(
                    builder,
                    "Query",
                    "foo",
                    new Func<object>(() => "bar"));

            // assert
            builder.Create()
                .MakeExecutable()
                .ExecuteAsync("{ foo }")
                .Result
                .MatchSnapshot();
        }
    }
}

