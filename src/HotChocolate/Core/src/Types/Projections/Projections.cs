#nullable enable

using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Projections;

internal sealed class SelectionExpressionBuilder
{
    public LambdaExpression BuildExpression<TRoot>(IOperation operation, ISelection selection)
    {
        var rootType = typeof(TRoot);
        var parameter = Expression.Parameter(rootType, "root");
        var context = new Context(operation, parameter, rootType);
        var selectionSet = context.GetSelectionSet(selection);
        var selectionSetExpression = BuildSelectionSetExpression(selectionSet, context);

        if (selectionSetExpression is null)
        {
            throw new InvalidOperationException("The selection set is empty.");
        }

        return Expression.Lambda<Func<TRoot, TRoot>>(selectionSetExpression, parameter);
    }

    private MemberInitExpression? BuildSelectionSetExpression(
        ISelectionSet selectionSet,
        Context context)
    {
        var assignments = ImmutableArray.CreateBuilder<MemberAssignment>();

        foreach (var selection in selectionSet.Selections)
        {
            var assignment = BuildSelectionExpression(selection, context);
            if (assignment is not null)
            {
                assignments.Add(assignment);
            }
        }

        if (assignments.Count == 0)
        {
            return null;
        }

        return Expression.MemberInit(
            Expression.New(context.ParentType),
            assignments.ToImmutable());
    }

    private MemberAssignment? BuildSelectionExpression(
        ISelection selection,
        Context context)
    {
        var namedType = selection.Field.Type.NamedType();

        if (namedType.IsAbstractType()
            || (selection.Field.Type.IsListType() && !namedType.IsLeafType()))
        {
            return null;
        }

        if (selection.Field.Member is not PropertyInfo property)
        {
            return null;
        }

        var propertyAccessor = Expression.Property(context.Parent, property);

        if (namedType.IsLeafType())
        {
            return Expression.Bind(property, propertyAccessor);
        }

        var selectionSet = context.GetSelectionSet(selection);
        var newContext = context with { Parent = propertyAccessor, ParentType = property.PropertyType };
        var selectionSetExpression = BuildSelectionSetExpression(selectionSet, newContext);
        return selectionSetExpression is null ? null : Expression.Bind(property, selectionSetExpression);
    }

    private readonly record struct Context(IOperation Operation, Expression Parent, Type ParentType)
    {
        public ISelectionSet GetSelectionSet(ISelection selection)
            => Operation.GetSelectionSet(selection, (ObjectType)selection.Type.NamedType());
    }
}


