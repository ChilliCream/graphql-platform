using System;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using Xunit;
using static HotChocolate.Tests.TestHelper;


namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorTests
    {
        [Fact]
        public async Task Create_Should_Throw_IfOperationHandlersIsNull()
        {
            // arrange

            var fooType = await CreateTypeAsync(new FooFilterType());

            Action action = () =>
            {
                new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default, null);
            };

            // act
            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task Create_Should_Throw_IfTypeConversionIsNull()
        {
            // arrange

            var fooType = await CreateTypeAsync(new FooFilterType());

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
        public async Task Create_Should_Throw_IfTypeIsNull()
        {
            // arrange

            var fooType = await CreateTypeAsync(new FooFilterType());

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
        public async Task Create_Should_Throw_IfInputTypeIsNull()
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
