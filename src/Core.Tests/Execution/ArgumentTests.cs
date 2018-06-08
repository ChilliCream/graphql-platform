using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
            List<int> ints = new List<int> { 1, 2, 3 };
            Schema schema = Schema.Create(c =>
            {
                c.RegisterQueryType(new ObjectType<Query>(d =>
                {
                    d.BindFields(BindingBehavior.Explicit);
                    d.Field(t => t.ListOfInt(ints))
                        .Name("a")
                        .Type<ObjectType<Bar>>()
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
            OperationExecuter operationExecuter = new OperationExecuter();
            QueryResult result = await operationExecuter.ExecuteRequestAsync(schema,
                Parser.Default.Parse("query x($x:[Int]) { a(foo:$x) { Foo} }"), null,
                new Dictionary<string, IValueNode> { { "x", list } },
                null, CancellationToken.None);

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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
