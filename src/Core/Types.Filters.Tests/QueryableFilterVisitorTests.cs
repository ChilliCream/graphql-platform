using System;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterVisitorTests
        : TypeTestBase
    {
        [Fact]
        public void Create_Equal_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new StringValueNode("a")));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(fooType, typeof(Foo));
            value.Accept(filter);

            // assert
            Assert.Equal(
                "(Bar = a AND (Bar = b AND Bar = c) AND (Bar = d OR Bar = e))",
                filter.Query);
        }

        [Fact]
        public void FooBar()
        {
            Expression<Func<Foo, bool>> x = c => c.Bar == "a" && ( c.Bar == "b" || c.Bar == "f" || c.Bar == "x");
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
            }
        }
    }
}
