using System;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    public class ExpressionVisitorConventionTestBase
        : TypeTestBase
    {
        public void AssertEnterAndLeave<T>(
            ObjectValueNode value,
            FilterKind kind,
            FilterFieldEnter enter,
            FilterFieldLeave leave)
        {
            FilterInputType<T> fooType = CreateType(new FilterInputType<T>());

            ExpressionVisitorMock helper =
                ExpressionVisitorMock.Create(enter)
                    .Setup(leave);

            FilterExpressionVisitorDefinition convention =
                new MockFilterConvention(
                    x => x.UseDefault()
                        .UseExpressionVisitor()
                        .UseDefault()
                            .Kind(kind)
                                .Enter(helper.FilterFieldEnter)
                                .Leave(helper.FilterFieldLeave))
                    .GetExpressionDefiniton();

            // act
            var filter = new QueryableFilterVisitorContext(
                fooType,
                typeof(T),
                convention,
                TypeConversion.Default,
                true);
            QueryableFilterVisitor.Default.Visit(value, filter);
            Func<T, bool> func = filter.CreateOrAssert<T>().Compile();

            // assert
            Assert.Equal(1, helper.CallCount(enter));
            Assert.Equal(1, helper.CallCount(leave));
        }

        public void AssertOperation<T>(
            ObjectValueNode value,
            FilterKind kind,
            FilterOperationKind operationKind,
            FilterOperationHandler handler)
        {
            FilterInputType<T> fooType = CreateType(new FilterInputType<T>());

            var helper = ExpressionVisitorMock.Create(handler);

            FilterExpressionVisitorDefinition convention =
                new MockFilterConvention(
                    x => x.UseDefault()
                        .UseExpressionVisitor()
                        .UseDefault()
                            .Kind(kind)
                                .Operation(operationKind)
                                    .Handler(helper.FilterOperationHandler))
                    .GetExpressionDefiniton();

            // act
            var filter = new QueryableFilterVisitorContext(
                fooType,
                typeof(T),
                convention,
                TypeConversion.Default,
                true);
            QueryableFilterVisitor.Default.Visit(value, filter);
            Func<T, bool> func = filter.CreateOrAssert<T>().Compile();

            // assert
            Assert.Equal(1, helper.CallCount(handler));
        }
    }
}
