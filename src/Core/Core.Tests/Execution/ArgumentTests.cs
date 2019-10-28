using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class ArgumentTests
    {
        [Fact]
        public async Task ListOfInt()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.Options.StrictValidation = true;

                c.RegisterQueryType(new ObjectType<Query>(d =>
                {
                    d.BindFields(BindingBehavior.Explicit);
                    d.Field(t => t.ListOfInt(default))
                        .Name("a")
                        .Type<ListType<ObjectType<Bar>>>()
                        .Argument("foo", a => a.Type<ListType<IntType>>());
                }));

                c.RegisterType(new ObjectType<Bar>());
            });

            var list = new ListValueNode(new List<IValueNode>
            {
                new IntValueNode(1),
                new IntValueNode(2),
                new IntValueNode(3)
            });

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "query x($x:[Int]) { a(foo:$x) { foo } }",
                    new Dictionary<string, object> { { "x", list } });

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ListOfIntLiterals()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.Options.StrictValidation = true;

                c.RegisterQueryType(new ObjectType<Query>(d =>
                {
                    d.BindFields(BindingBehavior.Explicit);
                    d.Field(t => t.ListOfInt(default))
                        .Name("a")
                        .Type<ListType<ObjectType<Bar>>>()
                        .Argument("foo", a => a.Type<ListType<IntType>>());
                }));

                c.RegisterType(new ObjectType<Bar>());
            });

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "query { a(foo:[1 2 3 4 5]) { foo } }");

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ListOfFoo()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.RegisterQueryType(new ObjectType<Query>(d =>
                {
                    d.BindFields(BindingBehavior.Explicit);
                    d.Field(t => t.ListOfFoo(default))
                        .Name("a")
                        .Type<ListType<ObjectType<Bar>>>()
                        .Argument("foo", a =>
                            a.Type<ListType<InputObjectType<Foo>>>());
                }));

                c.RegisterType(new ObjectType<Bar>());
                c.RegisterType(new InputObjectType<Foo>());
            });

            var list = new ListValueNode(new List<IValueNode>
            {
                new ObjectValueNode(new[] {
                    new ObjectFieldNode("bar", new StringValueNode("123")) }),
                new ObjectValueNode(new[] {
                    new ObjectFieldNode("bar", new StringValueNode("456")) }),
                new ObjectValueNode(new[] {
                    new ObjectFieldNode("bar", new StringValueNode("789")) }),
            });

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "query x($x:[FooInput]) { a(foo:$x) { foo } }",
                    new Dictionary<string, object> { { "x", list } });

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleInt()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.RegisterQueryType(new ObjectType<Query>(d =>
                {
                    d.BindFields(BindingBehavior.Explicit);
                    d.Field(t => t.SingleInt(default))
                        .Name("a")
                        .Type<ObjectType<Bar>>()
                        .Argument("foo", a => a.Type<IntType>());
                }));

                c.RegisterType(new ObjectType<Bar>());
            });

            var value = new IntValueNode(123);

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "query x($x:Int) { a(foo:$x) { foo } }",
                    new Dictionary<string, object> { { "x", value } });

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SingleFoo()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.RegisterQueryType(new ObjectType<Query>(d =>
                {
                    d.BindFields(BindingBehavior.Explicit);
                    d.Field(t => t.SingleFoo(default))
                        .Name("a")
                        .Type<ObjectType<Bar>>()
                        .Argument("foo", a => a.Type<InputObjectType<Foo>>());
                }));

                c.RegisterType(new ObjectType<Bar>());
                c.RegisterType(new InputObjectType<Foo>());
            });

            var obj = new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("bar", new StringValueNode("123"))
            });

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "query x($x:FooInput) { a(foo:$x) { foo } }",
                    new Dictionary<string, object> { { "x", obj } });

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task CachedVariablesAreNotAltered()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.RegisterQueryType(new ObjectType<Query>(d =>
                {
                    d.BindFields(BindingBehavior.Explicit);
                    d.Field(t => t.SingleFoo(default))
                        .Name("a")
                        .Type<ObjectType<Bar>>()
                        .Argument("foo", a => a.Type<InputObjectType<Foo>>());
                }));

                c.RegisterType(new ObjectType<Bar>());
                c.RegisterType(new InputObjectType<Foo>());
            });

            var obj = new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("bar", new StringValueNode("123"))
            });

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            await executor.ExecuteAsync(
                "query x { a(foo:{bar: \"abc\"}) { foo } }");

            IExecutionResult result =
                await executor.ExecuteAsync(
                    "query x { a(foo:{bar: \"abc\"}) { foo } }",
                    new Dictionary<string, object> { { "x", obj } });

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Extend_Argument_Coercion()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddSingleton<IArgumentCoercionHandler, ModifyStringHandler>();
            services.AddGraphQLSchema(builder => builder
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("bar")
                    .Argument("baz", a => a.Type<StringType>())
                    .Resolver(ctx => ctx.Argument<string>("baz"))));

            QueryExecutionBuilder
                .New()
                .UseDefaultPipeline()
                .Populate(services);

            IQueryExecutor executor = services.BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync("{ bar(baz: \"0\") }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Extend_Argument_Coercion_DefaultValue_String()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddSingleton<IArgumentCoercionHandler, ModifyStringHandler>();
            services.AddGraphQLSchema(builder => builder
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("bar")
                    .Argument("baz", a => a.Type<StringType>().DefaultValue("abc"))
                    .Resolver(ctx => ctx.Argument<string>("baz"))));

            QueryExecutionBuilder
                .New()
                .UseDefaultPipeline()
                .Populate(services);

            IQueryExecutor executor = services.BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync("{ bar }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Invalid_InputObject_Provided_As_Variable()
        {
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"input MyInput {
                        someField: String
                    }

                    type Query {
                        x(arg: MyInput): String
                    }")
                .Map(new FieldReference("Query", "x"),
                    next => ctx =>
                    {
                        ctx.Result = "Foo";
                        return Task.CompletedTask;
                    })
                .Create();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    @"query MyQuery($value: MyInput) {
                        x(arg: $value)
                    }")
                .AddVariableValue("value", new Dictionary<string, object>
                {
                    { "clearlyNonsense", "bar" }
                })
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IExecutionResult result = await executor.ExecuteAsync(request);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Extend_Argument_Coercion_With_Variables()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddSingleton<IArgumentCoercionHandler, ModifyStringHandler>();
            services.AddGraphQLSchema(builder => builder
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("bar")
                    .Argument("baz", a => a.Type<StringType>())
                    .Resolver(ctx => ctx.Argument<string>("baz"))));

            QueryExecutionBuilder
                .New()
                .UseDefaultPipeline()
                .Populate(services);

            IQueryExecutor executor = services.BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery("query foo($a: String) { bar(baz: $a) }")
                        .SetVariableValue("a", "abc")
                        .Create());

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Extend_Argument_Coercion_With_Variables_And_ObjectValue()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddSingleton<IArgumentCoercionHandler, ModifyStringHandler>();
            services.AddGraphQLSchema(builder => builder
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("bar")
                    .Argument("baz", a => a.Type<InputObjectType<Foo>>())
                    .Resolver(ctx => ctx.Argument<Foo>("baz").Bar)));

            QueryExecutionBuilder
                .New()
                .UseDefaultPipeline()
                .Populate(services);

            IQueryExecutor executor = services.BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery("query foo($a: String) { bar(baz: { bar: $a }) }")
                        .SetVariableValue("a", "abc")
                        .Create());

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Invalid_InputObject_SecondLevel_Provided_As_Variable()
        {
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"input MyInput {
                        someObj: SecondInput
                    }

                    input SecondInput {
                        someField: String
                    }

                    type Query {
                        x(arg: MyInput): String
                    }")
                .Map(new FieldReference("Query", "x"),
                    next => ctx =>
                    {
                        ctx.Result = "Foo";
                        return Task.CompletedTask;
                    })
                .Create();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    @"query MyQuery($value: MyInput) {
                        x(arg: $value)
                    }")
                .AddVariableValue("value", new Dictionary<string, object>
                {
                    { "someObj", new Dictionary<string, object>
                        {
                            { "clearlyNonsense", "baz" }
                        }
                    }
                })
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IExecutionResult result = await executor.ExecuteAsync(request);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Extend_Argument_Coercion_With_Variables_DefaultValue()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddSingleton<IArgumentCoercionHandler, ModifyStringHandler>();
            services.AddGraphQLSchema(builder => builder
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("bar")
                    .Argument("baz", a => a.Type<StringType>())
                    .Resolver(ctx => ctx.Argument<string>("baz"))));

            QueryExecutionBuilder
                .New()
                .UseDefaultPipeline()
                .Populate(services);

            IQueryExecutor executor = services.BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            "query foo($a: String = \"def\") { bar(baz: $a) }")
                        .Create());

            // assert
            result.MatchSnapshot();
        }

        public async Task Valid_InputObject_Provided_As_Variable()
        {
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"input MyInput {
                        someField: String
                    }

                    type Query {
                        x(arg: MyInput): String
                    }")
                .Map(new FieldReference("Query", "x"),
                    next => ctx =>
                    {
                        ctx.Result = "Foo";
                        return Task.CompletedTask;
                    })
                .Create();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    @"query MyQuery($value: MyInput) {
                        x(arg: $value)
                    }")
                .AddVariableValue("value", new Dictionary<string, object>
                {
                    { "someField", "bar" }
                })
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IExecutionResult result = await executor.ExecuteAsync(request);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task InputObject_As_Variable_Is_Converted_To_ObjectLiteral()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddSingleton<IArgumentCoercionHandler, ModifyObjectHandler>();
            services.AddGraphQLSchema(builder => builder
                .AddQueryType<Query>()
                .Create());

            QueryExecutionBuilder
                .New()
                .UseDefaultPipeline()
                .Populate(services);

            IQueryExecutor executor = services.BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery("query foo($a: FooInput) { singleFoo(foo: $a) { foo } }")
                        .AddVariableValue("a", new Dictionary<string, object>
                        {
                            { "bar", "TEST_123" }
                        })
                        .Create());

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Valid_InputObject_SecondLevel_Provided_As_Variable()
        {
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"input MyInput {
                        someObj: SecondInput
                    }

                    input SecondInput {
                        someField: String
                    }

                    type Query {
                        x(arg: MyInput): String
                    }")
                .Map(new FieldReference("Query", "x"),
                    next => ctx =>
                    {
                        ctx.Result = "Foo";
                        return Task.CompletedTask;
                    })
                .Create();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    @"query MyQuery($value: MyInput) {
                        x(arg: $value)
                    }")
                .AddVariableValue("value", new Dictionary<string, object>
                {
                    { "someObj", new Dictionary<string, object>
                        {
                            { "someField", "baz" }
                        }
                    }
                })
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IExecutionResult result = await executor.ExecuteAsync(request);

            result.MatchSnapshot();
        }

        public class Query
        {
            public Bar SingleFoo(Foo foo)
            {
                foo.Bar += "_";
                return new Bar { Foo = foo.Bar };
            }

            public Bar SingleInt(int foo)
            {
                return new Bar { Foo = foo.ToString() };
            }

            public List<Bar> ListOfFoo(List<Foo> foo)
            {
                var bar = new List<Bar>();
                foreach (Foo f in foo)
                {
                    bar.Add(SingleFoo(f));
                }
                return bar;
            }

            public List<Bar> ListOfInt(List<int> foo)
            {
                var bar = new List<Bar>();
                foreach (int f in foo)
                {
                    bar.Add(SingleInt(f));
                }
                return bar;
            }
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class Bar
        {
            public string Foo { get; set; }
        }

        public class ModifyStringHandler
            : IArgumentCoercionHandler
        {
            public object CoerceValue(IInputField argument, object value)
            {
                if (argument.Type is StringType && value is string s)
                {
                    return s + "123";
                }
                return value;
            }
        }

        public class ModifyObjectHandler
            : IArgumentCoercionHandler
        {
            public object CoerceValue(IInputField argument, object value)
            {
                if (value is ObjectValueNode o)
                {
                    var fields = o.Fields.ToList();
                    fields[0] = fields[0].WithValue(new StringValueNode("Bar_123"));
                    return o.WithFields(fields);
                }
                return value;
            }
        }
    }
}
