using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class ExpressionVisitorConventionTestBase
        : TypeTestBase
    {
        public void AssertEnterAndLeave<T>(
            ObjectValueNode value,
            object kind,
            FilterFieldEnter<Expression> enter,
            FilterFieldLeave<Expression> leave)
        {
            FilterInputType<T> fooType = CreateType(new FilterInputType<T>());

            ExpressionVisitorMock helper =
                ExpressionVisitorMock.Create(enter)
                    .Setup(leave);

            FilterVisitorDefinition<Expression> convention =
                new MockFilterConvention(
                    x => x.UseDefault()
                        .UseExpressionVisitor()
                        .UseDefault()
                            .Kind(kind)
                                .Enter(helper.FilterFieldEnter)
                                .Leave(helper.FilterFieldLeave))
                        .GetExpressionDefinition();

            // act
            var filter = new QueryableFilterVisitorContext(
                fooType,
                convention,
                TypeConversion.Default,
                true);
            FilterVisitor<Expression>.Default.Visit(value, filter);
            Func<T, bool> func = filter.CreateOrAssert<T>().Compile();

            // assert
            Assert.Equal(1, helper.CallCount(enter));
            Assert.Equal(1, helper.CallCount(leave));
        }

        public void AssertOperation<T>(
            ObjectValueNode value,
            object kind,
            object operationKind,
            FilterOperationHandler<Expression> handler)
        {
            FilterInputType<T> fooType = CreateType(new FilterInputType<T>());

            var helper = ExpressionVisitorMock.Create(handler);

            FilterVisitorDefinition<Expression> convention =
                new MockFilterConvention(
                    x => x.UseDefault()
                        .UseExpressionVisitor()
                        .UseDefault()
                            .Kind(kind)
                                .Operation(operationKind)
                                    .Handler(helper.FilterOperationHandler))
                    .GetExpressionDefinition();

            // act
            var filter = new QueryableFilterVisitorContext(
                fooType,
                convention,
                TypeConversion.Default,
                true);
            FilterVisitor<Expression>.Default.Visit(value, filter);
            Func<T, bool> func = filter.CreateOrAssert<T>().Compile();

            // assert
            Assert.Equal(1, helper.CallCount(handler));
        }
    }
}
