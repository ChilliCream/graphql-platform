using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Requirements;
using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Execution.Projections;

internal sealed class SelectionExpressionBuilder
{
    private static readonly HashSet<Type> s_runtimeLeafTypes =
    [
        typeof(string),
        typeof(byte),
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(float),
        typeof(byte),
        typeof(decimal),
        typeof(Guid),
        typeof(bool),
        typeof(char),
        typeof(byte?),
        typeof(short?),
        typeof(int?),
        typeof(long?),
        typeof(float?),
        typeof(byte?),
        typeof(decimal?),
        typeof(Guid?),
        typeof(bool?),
        typeof(char?)
    ];

    public Expression<Func<TRoot, TRoot>> BuildExpression<TRoot>(Selection selection)
    {
        var rootType = typeof(TRoot);
        var parameter = Expression.Parameter(rootType, "root");
        var requirements = selection.DeclaringOperation.Schema.Features.GetRequired<FieldRequirementsMetadata>();
        var context = new Context(parameter, rootType, requirements, new NullabilityInfoContext());
        var root = new TypeContainer();

        CollectTypes(context, selection, root);

        var selectionSetExpression = BuildTypeSwitchExpression(context, root);

        if (selectionSetExpression is null)
        {
            throw new InvalidOperationException("The selection set is empty.");
        }

        return Expression.Lambda<Func<TRoot, TRoot>>(selectionSetExpression, parameter);
    }

    public Expression<Func<TRoot, TRoot>> BuildNodeExpression<TRoot>(Selection selection)
    {
        var rootType = typeof(TRoot);
        var parameter = Expression.Parameter(rootType, "root");
        var requirements = selection.DeclaringOperation.Schema.Features.GetRequired<FieldRequirementsMetadata>();
        var context = new Context(parameter, rootType, requirements, new NullabilityInfoContext());
        var root = new TypeContainer();

        var entityType = selection.DeclaringOperation
            .GetPossibleTypes(selection)
            .Cast<ObjectType>()
            .FirstOrDefault(t => t.RuntimeType == typeof(TRoot));

        if (entityType is null)
        {
            throw new InvalidOperationException(
                $"Unable to resolve the entity type from `{typeof(TRoot).FullName}`.");
        }

        var typeNode = new TypeNode(entityType.RuntimeType);
        var selectionSet = selection.DeclaringOperation.GetSelectionSet(selection, entityType);
        CollectSelections(context, selectionSet, typeNode);
        root.TryAddNode(typeNode);

        if (typeNode.Nodes.Count == 0)
        {
            TryAddAnyLeafField(typeNode, entityType);
        }

        var selectionSetExpression = BuildTypeSwitchExpression(context, root);

        if (selectionSetExpression is null)
        {
            throw new InvalidOperationException("The selection set is empty.");
        }

        return Expression.Lambda<Func<TRoot, TRoot>>(selectionSetExpression, parameter);
    }

    private void CollectTypes(Context context, Selection selection, TypeContainer parent)
    {
        var namedType = selection.Type.NamedType();

        if (namedType.IsLeafType())
        {
            return;
        }

        if (namedType.IsAbstractType())
        {
            foreach (var possibleType in selection.DeclaringOperation.GetPossibleTypes(selection).Cast<ObjectType>())
            {
                var possibleTypeNode = new TypeNode(possibleType.RuntimeType);
                var possibleSelectionSet = selection.DeclaringOperation.GetSelectionSet(selection, possibleType);
                CollectSelections(context, possibleSelectionSet, possibleTypeNode);
                parent.TryAddNode(possibleTypeNode);

                if (possibleTypeNode.Nodes.Count == 0)
                {
                    TryAddAnyLeafField(possibleTypeNode, possibleType);
                }
            }

            return;
        }

        var objectType = (ObjectType)namedType;
        var typeNode = new TypeNode(objectType.RuntimeType);
        var selectionSet = selection.DeclaringOperation.GetSelectionSet(selection, (ObjectType)namedType);
        CollectSelections(context, selectionSet, typeNode);
        parent.TryAddNode(typeNode);

        if (typeNode.Nodes.Count == 0)
        {
            TryAddAnyLeafField(typeNode, objectType);
        }
    }

    private static Expression? BuildTypeSwitchExpression(
        Context context,
        TypeContainer parent)
    {
        if (parent.Nodes.Count > 1)
        {
            Expression switchExpression = Expression.Constant(null, context.ParentType);

            foreach (var typeNode in parent.Nodes)
            {
                var newParent = Expression.Convert(context.Parent, typeNode.Type);
                var newContext = context with { Parent = newParent, ParentType = typeNode.Type };
                var typeCondition = Expression.TypeIs(context.Parent, typeNode.Type);
                var selectionSet = BuildSelectionSetExpression(newContext, typeNode);

                if (selectionSet is null)
                {
                    throw new InvalidOperationException();
                }

                var castedSelectionSet = Expression.Convert(selectionSet, context.ParentType);
                switchExpression = Expression.Condition(typeCondition, castedSelectionSet, switchExpression);
            }

            return switchExpression;
        }

        return BuildSelectionSetExpression(context, parent.Nodes[0]);
    }

    private static MemberInitExpression? BuildSelectionSetExpression(
        Context context,
        TypeNode parent)
    {
        var assignments = ImmutableArray.CreateBuilder<MemberAssignment>();

        // order by property name so expressions evaluate to the same hash regardless of selection order
        foreach (var property in parent.Nodes.OrderBy(node => node.Property.Name))
        {
            var assignment = BuildAssignmentExpression(property, context);
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
        Selection selection,
        TypeNode parent)
    {
        var namedType = selection.Field.Type.NamedType();

        if (selection.Field.Type.IsListType() && !namedType.IsLeafType()
            || selection.Field.PureResolver is null
            || selection.Field.ResolverMember?.ReflectedType != selection.Field.DeclaringType.RuntimeType)
        {
            return;
        }

        if (selection.Field.Member is not PropertyInfo { CanRead: true, CanWrite: true } property)
        {
            return;
        }

        var flags = selection.Field.Flags;
        if ((flags & CoreFieldFlags.Connection) == CoreFieldFlags.Connection
            || (flags & CoreFieldFlags.CollectionSegment) == CoreFieldFlags.CollectionSegment)
        {
            return;
        }

        var propertyNode = parent.AddOrGetNode(property);

        if (namedType.IsLeafType())
        {
            return;
        }

        CollectTypes(context, selection, propertyNode);
    }

    private static void TryAddAnyLeafField(
        TypeNode parent,
        ObjectType selectionType)
    {
        // if we could not collect anything it means that either all fields
        // are skipped or that __typename is the only field that is selected.
        // in this case we will try to select the id field or if that does
        // not exist we will look for a leaf field that we can select.
        if (selectionType.Fields.TryGetField("id", out var idField)
            && idField.Member is PropertyInfo idProperty)
        {
            parent.AddOrGetNode(idProperty);
        }
        else
        {
            // if id does not exist we will try to select any leaf field from the type.
            var anyProperty = selectionType.Fields.FirstOrDefault(t => t.Type.IsLeafType() && t.Member is PropertyInfo);

            if (anyProperty?.Member is PropertyInfo anyPropertyInfo)
            {
                parent.AddOrGetNode(anyPropertyInfo);
            }
            else
            {
                // if we still have not found any leaf we will inspect the runtime type and
                // try to select any leaf property.
                var properties = selectionType.RuntimeType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var property in properties)
                {
                    if (s_runtimeLeafTypes.Contains(property.PropertyType))
                    {
                        parent.AddOrGetNode(property);
                        break;
                    }
                }
            }
        }
    }

    private void CollectSelections(
        Context context,
        SelectionSet selectionSet,
        TypeNode parent)
    {
        foreach (var selection in selectionSet.Selections)
        {
            var requirements = context.GetRequirements(selection);
            if (requirements is not null)
            {
                foreach (var requirement in requirements.Nodes)
                {
                    parent.TryAddNode(requirement.Clone());
                }
            }

            CollectSelection(context, selection, parent);
        }
    }

    private static MemberAssignment? BuildAssignmentExpression(
        PropertyNode node,
        Context context)
    {
        var propertyAccessor = Expression.Property(context.Parent, node.Property);

        if (node.Nodes.Count == 0)
        {
            if (IsNullableType(context, node.Property))
            {
                var nullCheck = Expression.Condition(
                    Expression.Equal(propertyAccessor, Expression.Constant(null)),
                    Expression.Constant(null, node.Property.PropertyType),
                    propertyAccessor);

                return Expression.Bind(node.Property, nullCheck);
            }

            return Expression.Bind(node.Property, propertyAccessor);
        }

        if (node.IsArrayOrCollection)
        {
            throw new NotSupportedException("List projections are not supported.");
        }

        var newContext = context with { Parent = propertyAccessor, ParentType = node.Property.PropertyType };
        var nestedExpression = BuildTypeSwitchExpression(newContext, node);

        if (IsNullableType(context, node.Property))
        {
            var nullCheck = Expression.Condition(
                Expression.Equal(propertyAccessor, Expression.Constant(null)),
                Expression.Constant(null, node.Property.PropertyType),
                nestedExpression ?? Expression.Constant(null, node.Property.PropertyType));

            return Expression.Bind(node.Property, nullCheck);
        }

        return nestedExpression is null ? null : Expression.Bind(node.Property, nestedExpression);
    }

    private static bool IsNullableType(Context context, PropertyInfo propertyInfo)
    {
        if (propertyInfo.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null;
        }

        var nullabilityInfo = context.NullabilityInfoContext.Create(propertyInfo);
        return nullabilityInfo.WriteState == NullabilityState.Nullable;
    }

    private readonly record struct Context(
        Expression Parent,
        Type ParentType,
        FieldRequirementsMetadata Requirements,
        NullabilityInfoContext NullabilityInfoContext)
    {
        public TypeNode? GetRequirements(Selection selection)
        {
            var flags = selection.Field.Flags;
            return (flags & CoreFieldFlags.WithRequirements) == CoreFieldFlags.WithRequirements
                ? Requirements.GetRequirements(selection.Field)
                : null;
        }
    }
}
