#if NET6_0_OR_GREATER
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Execution.Projections;

internal sealed class SelectionExpressionBuilder
{
    public Expression<Func<TRoot, TRoot>> BuildExpression<TRoot>(ISelection selection)
    {
        var rootType = typeof(TRoot);
        var parameter = Expression.Parameter(rootType, "root");
        var requirements = selection.DeclaringOperation.Schema.Features.GetRequired<FieldRequirementsMetadata>();
        var context = new Context(selection.DeclaringOperation, parameter, rootType, requirements);
        var root = new PropertyNodeContainer();
        var selectionSet = context.GetSelectionSet(selection);

        CollectSelections(context, selectionSet, root);

        if (root.Nodes.Count == 0)
        {
            TryAddAnyLeafField(selection, root);
        }

        var selectionSetExpression = BuildSelectionSetExpression(context, root);

        if (selectionSetExpression is null)
        {
            throw new InvalidOperationException("The selection set is empty.");
        }

        return Expression.Lambda<Func<TRoot, TRoot>>(selectionSetExpression, parameter);
    }

    private MemberInitExpression? BuildSelectionSetExpression(
        Context context,
        PropertyNodeContainer parent)
    {
        var assignments = ImmutableArray.CreateBuilder<MemberAssignment>();

        foreach (var property in parent.Nodes)
        {
            var assignment = BuildExpression(property, context);
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

    private void CollectSelection(
        Context context,
        ISelection selection,
        PropertyNodeContainer parent)
    {
        var namedType = selection.Field.Type.NamedType();

        if (namedType.IsAbstractType()
            || selection.Field.Type.IsListType() && !namedType.IsLeafType()
            || selection.Field.PureResolver is null
            || selection.Field.ResolverMember?.ReflectedType != selection.Field.DeclaringType.RuntimeType)
        {
            return;
        }

        if (selection.Field.Member is not PropertyInfo property)
        {
            return;
        }

        var flags = ((ObjectField)selection.Field).Flags;
        if ((flags & FieldFlags.Connection) == FieldFlags.Connection
            || (flags & FieldFlags.CollectionSegment) == FieldFlags.CollectionSegment)
        {
            return;
        }

        var propertyNode = parent.AddOrGetNode(property);

        if (namedType.IsLeafType())
        {
            return;
        }

        var selectionSet = context.GetSelectionSet(selection);
        CollectSelections(context, selectionSet, propertyNode);

        if (propertyNode.Nodes.Count > 0)
        {
            return;
        }

        TryAddAnyLeafField(selection, propertyNode);
    }

    private static void TryAddAnyLeafField(
        ISelection selection,
        PropertyNodeContainer parent)
    {
        // if we could not collect anything it means that either all fields
        // are skipped or that __typename is the only field that is selected.
        // in this case we will try to select the id field or if that does
        // not exist we will look for a leaf field that we can select.
        var type = (ObjectType)selection.Type.NamedType();
        if (type.Fields.TryGetField("id", out var idField)
            && idField.Member is PropertyInfo idProperty)
        {
            parent.AddOrGetNode(idProperty);
        }
        else
        {
            var anyProperty = type.Fields.FirstOrDefault(t => t.Type.IsLeafType() && t.Member is PropertyInfo);
            if (anyProperty?.Member is PropertyInfo anyPropertyInfo)
            {
                parent.AddOrGetNode(anyPropertyInfo);
            }
        }
    }

    private void CollectSelections(
        Context context,
        ISelectionSet selectionSet,
        PropertyNodeContainer parent)
    {
        foreach (var selection in selectionSet.Selections)
        {
            var requirements = context.GetRequirements(selection);
            if (requirements is not null)
            {
                foreach (var requirement in requirements)
                {
                    parent.AddNode(requirement.Clone());
                }
            }

            CollectSelection(context, selection, parent);
        }
    }

    private MemberAssignment? BuildExpression(
        PropertyNode node,
        Context context)
    {
        var propertyAccessor = Expression.Property(context.Parent, node.Property);

        if (node.Nodes.Count == 0)
        {
            return Expression.Bind(node.Property, propertyAccessor);
        }

        if(node.IsArrayOrCollection)
        {
            throw new NotSupportedException("List projections are not supported.");
        }

        var newContext = context with { Parent = propertyAccessor, ParentType = node.Property.PropertyType };
        var requirementsExpression = BuildExpression(node.Nodes, newContext);
        return requirementsExpression is null ? null : Expression.Bind(node.Property, requirementsExpression);
    }

    private MemberInitExpression? BuildExpression(
        IReadOnlyList<PropertyNode> properties,
        Context context)
    {
        var allAssignments = ImmutableArray.CreateBuilder<MemberAssignment>();

        foreach (var property in properties)
        {
            var assignment = BuildExpression(property, context);
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
        {
            var flags = ((ObjectField)selection.Field).Flags;
            return (flags & FieldFlags.WithRequirements) == FieldFlags.WithRequirements
                ? Requirements.GetRequirements(selection.Field)
                : null;
        }

        public ISelectionSet GetSelectionSet(ISelection selection)
            => Operation.GetSelectionSet(selection, (ObjectType)selection.Type.NamedType());
    }
}
#endif
