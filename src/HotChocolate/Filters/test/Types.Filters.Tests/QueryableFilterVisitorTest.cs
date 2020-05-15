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
                typeof(Foo),
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
                typeof(Foo),
                MockFilterConvention.Default.GetExpressionDefinition(),
                null,
                true);
            };

            // act
            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_Should_Throw_IfTypeIsNull()
        {
            // arrange

            FooFilterType fooType = CreateType(new FooFilterType());

            Action action = () =>
            {
                new QueryableFilterVisitorContext(
                fooType,
                null,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default,
                true);
            };

            // act
            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_Should_Throw_IfInputTypeIsNull()
        {
            // arrange 

            Action action = () =>
            {
                new QueryableFilterVisitorContext(
                null,
                typeof(Foo),
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default,
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
