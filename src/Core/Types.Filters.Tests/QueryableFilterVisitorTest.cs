using System;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorTests
        : TypeTestBase
    {
        [Fact]
        public void Create_Should_Throw_IfOperationHandlersIsNull()
        {
            // arrange

            var fooType = CreateType(new FooFilterType());

            Action action = () =>
            {
                new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default, null, ExpressionFieldHandlers.All);
            };

            // act
            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_Should_Throw_IfFieldHandlersIsNull()
        {
            // arrange

            var fooType = CreateType(new FooFilterType());

            Action action = () =>
            {
                new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default, ExpressionOperationHandlers.All, null);
            };

            // act
            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_Should_Throw_IfTypeConversionIsNull()
        {
            // arrange

            var fooType = CreateType(new FooFilterType());

            Action action = () =>
            {
                new QueryableFilterVisitor(
                fooType, typeof(Foo), null);
            };

            // act
            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_Should_Throw_IfTypeIsNull()
        {
            // arrange

            var fooType = CreateType(new FooFilterType());

            Action action = () =>
            {
                new QueryableFilterVisitor(
                fooType, null, TypeConversion.Default);
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
                new QueryableFilterVisitor(
                null, typeof(Foo), TypeConversion.Default);
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
