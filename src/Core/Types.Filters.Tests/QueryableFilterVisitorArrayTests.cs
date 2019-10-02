using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorArayyTests
        : TypeTestBase
    {
        [Fact]
        public void Create_ArrayObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_some",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")
                        )
                    )
                )
            );

            var fooType = CreateType(new FooFilterType());
             
            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { FooNested = new[] { new FooNested { Bar = "c" }, new FooNested { Bar = "d" }, new FooNested { Bar = "a" } } };
            Assert.True(func(a));

            var b = new Foo { FooNested = new[] { new FooNested { Bar = "c" }, new FooNested { Bar = "d" }, new FooNested { Bar = "b" } } };
            Assert.False(func(b));
        }


        public class Foo
        {
            public IEnumerable<FooNested> FooNested { get; set; }
        }

        public class FooNested
        {
            public string Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Filter(t => t.FooNested).AllowSome();
            }
        }

    }
}
