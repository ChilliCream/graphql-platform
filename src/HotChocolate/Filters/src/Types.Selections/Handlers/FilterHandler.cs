using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Selections.Handlers
{
    public class FilterHandler : IListHandler
    {
        public Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression)
        {
            IFilterConvention convention = context.SelectionContext.FilterConvention;
            NameString argumentName = convention.GetArgumentName();
            if (context.TryGetValueNode(argumentName, out IValueNode? filter) &&
                selection.Field.Arguments[argumentName].Type is InputObjectType iot &&
                iot is IFilterInputType fit &&
                convention.TryGetVisitorDefinition(
                    out FilterVisitorDefinitionBase? defintion) &&
                defintion is FilterVisitorDefinition<Expression> expressionDefinition)
            {
                var visitorContext = new QueryableFilterVisitorContext(
                    fit,
                    expressionDefinition,
                    context.Conversion,
                    false);

                FilterVisitor<Expression>.Default.Visit(filter, visitorContext);

                if (visitorContext.TryCreateLambda(
                    out LambdaExpression? filterExpression))
                {
                    return Expression.Call(
                        typeof(Enumerable),
                        nameof(Enumerable.Where),
                        new[] { fit.EntityType },
                        expression,
                        filterExpression);
                }
                else
                {
                    context.ReportErrors(visitorContext.Errors);
                }
            }

            return expression;
        }
    }
}
