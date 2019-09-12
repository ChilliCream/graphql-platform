using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
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
            public IValueNode PrepareValue(IInputField argument, IValueNode literal)
            {
                if (argument.Type is StringType && literal is StringValueNode s)
                {
                    return s.WithValue(s.Value + "123");
                }
                return literal;
            }

            public object CoerceValue(IInputField argument, object value)
            {
                if (argument.Type is StringType && value is string s)
                {
                    return s + "456";
                }
                return value;
            }
        }
    }
}
