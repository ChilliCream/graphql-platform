using System;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorContextTests
        : TypeTestBase
    {
        [Fact]
        public void Create_Should_Throw_IfOperationDefinitionIsNull()
        {
            // arrange

            FooFilterType fooType = CreateType(new FooFilterType());

            Action action = () =>
            {
                new QueryableFilterVisitorContext(
                fooType,
                null,
                TypeConversion.Default,
                true);
            };

            // act
            // assert
            Assert.Throws<ArgumentNullException>(action);
        }


        [Fact]
        public void Create_Should_Throw_IfTypeConversionIsNull()
        {
            // arrange

            FooFilterType fooType = CreateType(new FooFilterType());

            Action action = () =>
            {
                new QueryableFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                null,
                true);
            };

            // act
            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        public class Foo
        {
            public bool Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Filter(t => t.Bar)
                    .AllowEquals().And().AllowNotEquals();
            }
        }
    }
}
