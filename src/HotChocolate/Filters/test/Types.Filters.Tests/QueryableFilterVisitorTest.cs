using System;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Filters;

[Obsolete]
public class QueryableFilterVisitorContextTests
    : TypeTestBase
{
    [Fact]
    public void Create_Should_Throw_IfOperationHandlersIsNull()
    {
        // arrange

        var fooType = CreateType(new FooFilterType());

        var action = () =>
        {
            new QueryableFilterVisitorContext(
                fooType,
                typeof(Foo),
                null!,
                ExpressionFieldHandlers.All,
                DefaultTypeConverter.Default,
                true,
                new(new DefaultTypeConverter()));
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

        var action = () =>
        {
            new QueryableFilterVisitorContext(
                fooType,
                typeof(Foo),
                ExpressionOperationHandlers.All,
                null!,
                DefaultTypeConverter.Default,
                true,
                new(new DefaultTypeConverter()));
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

        var action = () =>
        {
            new QueryableFilterVisitorContext(
                fooType,
                typeof(Foo),
                null!,
                true,
                new(new DefaultTypeConverter()));
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

        var action = () =>
        {
            new QueryableFilterVisitorContext(
                fooType,
                null!,
                DefaultTypeConverter.Default,
                true,
                new(new DefaultTypeConverter()));
        };

        // act
        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Create_Should_Throw_IfInputTypeIsNull()
    {
        // arrange

        var action = () =>
        {
            new QueryableFilterVisitorContext(
                null!,
                typeof(Foo),
                DefaultTypeConverter.Default,
                true,
                new(new DefaultTypeConverter()));
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
                .AllowEquals()
                .And()
                .AllowNotEquals();
        }
    }
}