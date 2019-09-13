using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class AnyTypeTests
    {
        [Fact]
        public async Task Output_Return_Object()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolver(ctx => new Foo()))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Output_Return_List()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolver(ctx => new List<Foo> { new Foo() }))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Output_Return_DateTime()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolver(ctx => new DateTimeOffset(
                        new DateTime(2016, 01, 01),
                        TimeSpan.Zero)))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Output_Return_String()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolver(ctx => "abc"))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Output_Return_Int()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolver(ctx => 123))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Output_Return_Float()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolver(ctx => 1.2))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Output_Return_Boolean()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolver(ctx => true))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Object()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolver(ctx => ctx.Argument<object>("input")))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ foo(input: { a: \"foo\" }) }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Value_List()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolver(ctx => ctx.Argument<object>("input")))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ foo(input: [ \"foo\" ]) }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Object_List()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolver(ctx => ctx.Argument<object>("input")))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ foo(input: [ { a: \"foo\" } ]) }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Value_Object_To_Foo()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolver(ctx => ctx.Argument<Foo>("input")))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ foo(input: { bar: { baz: \"FooBar\" } }) }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        public class Foo
        {
            public Bar Bar { get; set; } = new Bar();
        }

        public class Bar
        {
            public string Baz { get; set; } = "Baz";
        }
    }
}
