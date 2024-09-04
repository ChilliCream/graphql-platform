#if NET6_0_OR_GREATER
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Relay.Descriptors;

namespace HotChocolate.Execution.Projections;

internal sealed class SelectionExpressionBuilder
{
    public Expression<Func<TRoot, TRoot>> BuildExpression<TRoot>(ISelection selection)
    {
        var rootType = typeof(TRoot);
        var parameter = Expression.Parameter(rootType, "root");
        var requirements = selection.DeclaringOperation.Schema.Features.GetRequired<FieldRequirementsMetadata>();
        var context = new Context(selection.DeclaringOperation, parameter, rootType, requirements);
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
        var allAssignments = ImmutableArray.CreateBuilder<MemberAssignment>();
        var allRequirements = ImmutableList<ImmutableArray<PropertyNode>>.Empty;

        foreach (var selection in selectionSet.Selections)
        {
            var requirements = context.GetRequirements(selection);
            if (requirements is not null)
            {
                allRequirements = allRequirements.Add(requirements.Value);
            }

            var assignment = BuildSelectionExpression(selection, context);
            if (assignment is not null)
            {
                allAssignments.Add(assignment);
            }
        }

        foreach (var properties in allRequirements)
        {
            foreach (var property in properties)
            {
                var assignment = BuildRequirementExpression(property, context);
                if (assignment is not null)
                {
                    allAssignments.Add(assignment);
                }
            }
        }

        if (allAssignments.Count == 0)
        {
            return null;
        }

        return Expression.MemberInit(
            Expression.New(context.ParentType),
            allAssignments.ToImmutable());
    }

    private MemberAssignment? BuildSelectionExpression(
        ISelection selection,
        Context context)
    {
        var namedType = selection.Field.Type.NamedType();

        if (namedType.IsAbstractType()
            || (selection.Field.Type.IsListType() && !namedType.IsLeafType())
            || selection.Field.ResolverMember?.ReflectedType != selection.Field.DeclaringType.RuntimeType)
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

    private MemberAssignment? BuildRequirementExpression(
        PropertyNode node,
        Context context)
    {
        var propertyAccessor = Expression.Property(context.Parent, node.Property);

        if (node.Nodes.Length == 0)
        {
            return Expression.Bind(node.Property, propertyAccessor);
        }

        var newContext = context with { Parent = propertyAccessor, ParentType = node.Property.PropertyType };
        var requirementsExpression = BuildRequirementsExpression(node.Nodes, newContext);
        return requirementsExpression is null ? null : Expression.Bind(node.Property, requirementsExpression);
    }

    private MemberInitExpression? BuildRequirementsExpression(
        ImmutableArray<PropertyNode> properties,
        Context context)
    {
        var allAssignments = ImmutableArray.CreateBuilder<MemberAssignment>();

        foreach (var property in properties)
        {
            var assignment = BuildRequirementExpression(property, context);
            if (assignment is not null)
            {
                allAssignments.Add(assignment);
            }
        }

        if (allAssignments.Count == 0)
        {
            return null;
        }

        return Expression.MemberInit(
            Expression.New(context.ParentType),
            allAssignments.ToImmutable());
    }

    private readonly record struct Context(
        IOperation Operation,
        Expression Parent,
        Type ParentType,
        FieldRequirementsMetadata Requirements)
    {
        public ImmutableArray<PropertyNode>? GetRequirements(ISelection selection)
            => Requirements.GetRequirements(selection.Field);

        public ISelectionSet GetSelectionSet(ISelection selection)
            => Operation.GetSelectionSet(selection, (ObjectType)selection.Type.NamedType());
    }
}

public sealed class PropertyNode(
    PropertyInfo property,
    ImmutableArray<PropertyNode> nodes)
{
    public PropertyInfo Property { get; } = property;

    public ImmutableArray<PropertyNode> Nodes { get; } = nodes;
}

public sealed class FieldRequirementsMetadata
{
    public ImmutableArray<PropertyNode>? GetRequirements(IObjectField field)
    {
        return null;
    }
}

#endif
