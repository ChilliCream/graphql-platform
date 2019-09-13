using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
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

        [Fact]
        public async Task Input_Value_String()
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
                "{ foo(input: \"foo\") }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Value_Int()
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
                "{ foo(input: 123) }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Value_Float()
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
                "{ foo(input: 1.2) }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Value_Boolean()
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
                "{ foo(input: true) }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Value_Null()
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
                "{ foo(input: null) }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Value_List_As_Variable()
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
                QueryRequestBuilder.New()
                    .SetQuery("query ($foo: Any) { foo(input: $foo) }")
                    .SetVariableValue("foo", new List<object> { "abc" })
                    .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Object_List_As_Variable()
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
                QueryRequestBuilder.New()
                    .SetQuery("query ($foo: Any) { foo(input: $foo) }")
                    .SetVariableValue("foo", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "abc", "def" }
                        }
                    })
                    .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Value_String_As_Variable()
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
               QueryRequestBuilder.New()
                   .SetQuery("query ($foo: Any) { foo(input: $foo) }")
                   .SetVariableValue("foo", "bar")
                   .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Value_Int_As_Variable()
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
               QueryRequestBuilder.New()
                   .SetQuery("query ($foo: Any) { foo(input: $foo) }")
                   .SetVariableValue("foo", 123)
                   .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Value_Float_As_Variable()
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
               QueryRequestBuilder.New()
                   .SetQuery("query ($foo: Any) { foo(input: $foo) }")
                   .SetVariableValue("foo", 1.2)
                   .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Value_Boolean_As_Variable()
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
               QueryRequestBuilder.New()
                   .SetQuery("query ($foo: Any) { foo(input: $foo) }")
                   .SetVariableValue("foo", false)
                   .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_Value_Null_As_Variable()
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
               QueryRequestBuilder.New()
                   .SetQuery("query ($foo: Any) { foo(input: $foo) }")
                   .SetVariableValue("foo", null)
                   .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public void IsInstanceOfType_EnumValue_False()
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

            AnyType type = schema.GetType<AnyType>("Any");

            // act
            bool result = type.IsInstanceOfType(new EnumValueNode("foo"));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_ObjectValue_True()
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

            AnyType type = schema.GetType<AnyType>("Any");

            // act
            bool result = type.IsInstanceOfType(
                new ObjectValueNode(
                    Array.Empty<ObjectFieldNode>()));

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_ListValue_False()
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

            AnyType type = schema.GetType<AnyType>("Any");

            // act
            bool result = type.IsInstanceOfType(
                new ListValueNode(
                    Array.Empty<IValueNode>()));

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_StringValue_False()
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

            AnyType type = schema.GetType<AnyType>("Any");

            // act
            bool result = type.IsInstanceOfType(new StringValueNode("foo"));

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_IntValue_False()
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

            AnyType type = schema.GetType<AnyType>("Any");

            // act
            bool result = type.IsInstanceOfType(new IntValueNode("123"));

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_FloatValue_False()
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

            AnyType type = schema.GetType<AnyType>("Any");

            // act
            bool result = type.IsInstanceOfType(new FloatValueNode("1.2"));

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_BooleanValue_False()
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

            AnyType type = schema.GetType<AnyType>("Any");

            // act
            bool result = type.IsInstanceOfType(new BooleanValueNode(true));

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_NullValue_True()
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

            AnyType type = schema.GetType<AnyType>("Any");

            // act
            bool result = type.IsInstanceOfType(NullValueNode.Default);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_Null_ArgumentNullException()
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

            AnyType type = schema.GetType<AnyType>("Any");

            // act
            Action action = () => type.IsInstanceOfType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
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
