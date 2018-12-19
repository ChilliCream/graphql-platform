using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution
{
    public class ArgumentTests
    {
        [Fact]
        public async Task ListOfInt()
        {
            // arrange
            Schema schema = Schema.Create(c =>
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

            ListValueNode list = new ListValueNode(new List<IValueNode>
            {
                new IntValueNode(1),
                new IntValueNode(2),
                new IntValueNode(3)
            });

            // act
            IExecutionResult result = await schema.ExecuteAsync(
                "query x($x:[Int]) { a(foo:$x) { foo } }",
                new Dictionary<string, object> { { "x", list } });

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task ListOfFoo()
        {
            // arrange
            Schema schema = Schema.Create(c =>
            {
                c.RegisterQueryType(new ObjectType<Query>(d =>
                {
                    d.BindFields(BindingBehavior.Explicit);
                    d.Field(t => t.ListOfFoo(default))
                        .Name("a")
                        .Type<ListType<ObjectType<Bar>>>()
                        .Argument("foo", a => a.Type<ListType<InputObjectType<Foo>>>());
                }));

                c.RegisterType(new ObjectType<Bar>());
                c.RegisterType(new InputObjectType<Foo>());
            });

            ListValueNode list = new ListValueNode(new List<IValueNode>
            {
                new ObjectValueNode(new[] {
                    new ObjectFieldNode("bar", new StringValueNode("123")) }),
                new ObjectValueNode(new[] {
                    new ObjectFieldNode("bar", new StringValueNode("456")) }),
                new ObjectValueNode(new[] {
                    new ObjectFieldNode("bar", new StringValueNode("789")) }),
            });

            // act
            IExecutionResult result = await schema.ExecuteAsync(
                "query x($x:[FooInput]) { a(foo:$x) { foo } }",
                new Dictionary<string, object> { { "x", list } });

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task SingleInt()
        {
            // arrange
            Schema schema = Schema.Create(c =>
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

            IntValueNode value = new IntValueNode(123);

            // act
            IExecutionResult result = await schema.ExecuteAsync(
                "query x($x:Int) { a(foo:$x) { foo } }",
                new Dictionary<string, object> { { "x", value } });

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task SingleFoo()
        {
            // arrange
            Schema schema = Schema.Create(c =>
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

            ObjectValueNode obj = new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("bar", new StringValueNode("123"))
            });

            // act
            IExecutionResult result = await schema.ExecuteAsync(
                "query x($x:FooInput) { a(foo:$x) { foo } }",
                new Dictionary<string, object> { { "x", obj } });

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }


        public class Query
        {
            public Bar SingleFoo(Foo foo)
            {
                return new Bar { Foo = foo.Bar };
            }

            public Bar SingleInt(int foo)
            {
                return new Bar { Foo = foo.ToString() };
            }

            public List<Bar> ListOfFoo(List<Foo> foo)
            {
                List<Bar> bar = new List<Bar>();
                foreach (Foo f in foo)
                {
                    bar.Add(SingleFoo(f));
                }
                return bar;
            }

            public List<Bar> ListOfInt(List<int> foo)
            {
                List<Bar> bar = new List<Bar>();
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
    }
}
