using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    private static readonly MethodInfo s_selectMethod =
        typeof(Enumerable)
            .GetMethods()
            .Where(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2)
            .First(m => m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>));
    private static readonly MethodInfo s_toListMethod =
        typeof(Enumerable)
            .GetMethods()
            .Where(m => m.Name == nameof(Enumerable.ToList) && m.GetParameters().Length == 1)
            .Single();
    private static readonly MethodInfo s_toArrayMethod =
        typeof(Enumerable)
            .GetMethods()
            .Where(m => m.Name == nameof(Enumerable.ToArray) && m.GetParameters().Length == 1)
            .Single();

    public Expression<Func<TRoot, TRoot>> BuildExpression<TRoot>(
        Selection selection,
        ulong includeFlags)
        => BuildExpression<TRoot>(selection, includeFlags, out _);

    public Expression<Func<TRoot, TRoot>> BuildExpression<TRoot>(
        Selection selection,
        ulong includeFlags,
        out ulong dependencyMask)
    {
        var rootType = typeof(TRoot);
        var parameter = Expression.Parameter(rootType, "root");
        var requirements = selection.DeclaringOperation.Schema.Features.GetRequired<FieldRequirementsMetadata>();
        var mask = new DependencyMask();
        var context = new Context(parameter, rootType, requirements, new NullabilityInfoContext(), includeFlags, mask);
        var root = new TypeContainer();

        CollectTypes(context, selection, root);
        dependencyMask = mask.Value;

        var selectionSetExpression = BuildTypeSwitchExpression(context, root);

        if (selectionSetExpression is null)
        {
            throw new InvalidOperationException("The selection set is empty.");
        }

        return Expression.Lambda<Func<TRoot, TRoot>>(selectionSetExpression, parameter);
    }

    public Expression<Func<TRoot, TRoot>> BuildNodeExpression<TRoot>(
        Selection selection,
        ulong includeFlags)
        => BuildNodeExpression<TRoot>(selection, includeFlags, out _);

    public Expression<Func<TRoot, TRoot>> BuildNodeExpression<TRoot>(
        Selection selection,
        ulong includeFlags,
        out ulong dependencyMask)
    {
        var rootType = typeof(TRoot);
        var parameter = Expression.Parameter(rootType, "root");
        var requirements = selection.DeclaringOperation.Schema.Features.GetRequired<FieldRequirementsMetadata>();
        var mask = new DependencyMask();
        var context = new Context(parameter, rootType, requirements, new NullabilityInfoContext(), includeFlags, mask);
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
        dependencyMask = mask.Value;
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

                // If a type condition only selects non-bindable fields like __typename,
                // BuildSelectionSetExpression returns null. Reuse the source instance
                // instead so the branch remains query-parameter dependent and does not
                // get parameterized as a constant by EF.
                selectionSet ??= newParent;

                var castedSelectionSet = Expression.Convert(selectionSet, context.ParentType);
                switchExpression = Expression.Condition(typeCondition, castedSelectionSet, switchExpression);
            }

            return switchExpression;
        }

        var singleTypeNode = parent.Nodes[0];

        if (context.ParentType != singleTypeNode.Type)
        {
            var newParent = Expression.Convert(context.Parent, singleTypeNode.Type);
            var newContext = context with { Parent = newParent, ParentType = singleTypeNode.Type };
            var selectionSet = BuildSelectionSetExpression(newContext, singleTypeNode);

            if (selectionSet is null)
            {
                return null;
            }

            var castedSelectionSet = Expression.Convert(selectionSet, context.ParentType);

            return Expression.Condition(
                Expression.TypeIs(context.Parent, singleTypeNode.Type),
                castedSelectionSet,
                Expression.Constant(null, context.ParentType));
        }

        return BuildSelectionSetExpression(context, singleTypeNode);
    }

    private static Expression? BuildSelectionSetExpression(
        Context context,
        TypeNode parent)
    {
        var members = ImmutableArray.CreateBuilder<(PropertyInfo Property, Expression Value)>();

        // order by property name so expressions evaluate to the same hash regardless of selection order
        foreach (var property in parent.Nodes.OrderBy(node => node.Property.Name))
        {
            var value = BuildMemberValueExpression(property, context);
            if (value is not null)
            {
                members.Add((property.Property, value));
            }
        }

        if (members.Count == 0)
        {
            return null;
        }

        var memberList = members.ToImmutable();

        // We keep EF constructor-injected entities intact by reusing the existing instance.
        if (ShouldReuseExistingInstance(context.ParentType))
        {
            return context.Parent;
        }

        var writableMembers = memberList.Where(m => m.Property.CanWrite).ToImmutableArray();
        var readOnlyMembers = memberList.Where(m => !m.Property.CanWrite).ToImmutableArray();

        // A selection made up entirely of writable members behaves exactly as before this
        // type ever had to consider read-only properties: bind everything through MemberInit
        // on a parameterless constructor, or fall back to a covering constructor.
        if (readOnlyMembers.Length == 0)
        {
            return BuildFromWritableMembers(context, writableMembers);
        }

        // A read-only navigation or collection member (for example one injected by a field
        // requirement, which bypasses the leaf-scalar selection gate) can neither be fed to a
        // constructor nor bound through MemberInit. Reuse the existing instance so that data
        // stays loaded instead of silently dropping it.
        if (readOnlyMembers.Any(m => !IsLeafScalarAutoProperty(m.Property)))
        {
            return context.Parent;
        }

        // Read-only leaf properties can only be populated through a constructor. The
        // constructor is chosen per runtime type rather than per selection, so that sibling
        // selectors for the same type (for example the "nodes" and "edges.node" selectors of
        // a paged field) always agree on the same constructor and can be merged safely.
        var coveringConstructor = FindCoveringConstructor(context.ParentType);

        if (coveringConstructor.Constructor is not null)
        {
            var arguments = coveringConstructor.Parameters.Select(p =>
                BuildConstructorArgument(context.Parent, context.ParentType, p));

            // Writable members are bound after the constructor runs, so they always win over
            // whatever the constructor itself computes from its arguments.
            return Expression.MemberInit(
                Expression.New(coveringConstructor.Constructor, arguments),
                writableMembers.Select(m => Expression.Bind(m.Property, m.Value)));
        }

        // The selection contains only leaf members (no nested object or collection
        // projection selected below it), so reusing the source instance loses no nested
        // projection and carries the correct read-only values. This may fetch more columns
        // than strictly required.
        if (parent.Nodes.All(n => n.Nodes.Count == 0))
        {
            return context.Parent;
        }

        // The selection also projects a nested object or collection, and no constructor
        // covers every read-only leaf on this type. Reusing the source instance here would
        // drop that nested projection on a database-backed source, so the read-only leaf
        // scalars are dropped instead and only the writable subset is projected.
        return writableMembers.Length == 0
            ? context.Parent
            : BuildFromWritableMembers(context, writableMembers);
    }

    // Pre-diff behavior for a fully writable member set: bind each selected member on a new
    // instance, or (for record-like types without a parameterless constructor) construct
    // through the smallest constructor that covers every member, defaulting any argument that
    // is not selected.
    private static Expression BuildFromWritableMembers(
        Context context,
        ImmutableArray<(PropertyInfo Property, Expression Value)> memberList)
    {
        var parameterlessConstructor = context.ParentType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        if (parameterlessConstructor is not null)
        {
            return Expression.MemberInit(
                Expression.New(parameterlessConstructor),
                memberList.Select(m => Expression.Bind(m.Property, m.Value)));
        }

        // Fallback path for record-like types without a parameterless constructor.
        var bestMatchingConstructor = context.ParentType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(c => !IsRecordCopyConstructor(c, context.ParentType))
            .Select(c => (Constructor: c, Parameters: c.GetParameters()))
            .OrderBy(c => c.Parameters.Length)
            .FirstOrDefault(c =>
                c.Parameters.Length >= memberList.Length
                && memberList.All(m =>
                    c.Parameters.Any(p =>
                        string.Equals(m.Property.Name, p.Name, StringComparison.OrdinalIgnoreCase)
                        && m.Value.Type.IsAssignableTo(p.ParameterType))));

        if (bestMatchingConstructor.Constructor is not null)
        {
            var arguments = bestMatchingConstructor.Parameters.Select(p =>
            {
                var member = memberList.FirstOrDefault(m =>
                    string.Equals(m.Property.Name, p.Name, StringComparison.OrdinalIgnoreCase)
                    && m.Value.Type.IsAssignableTo(p.ParameterType));

                if (member.Value is not null)
                {
                    return member.Value.Type == p.ParameterType
                        ? member.Value
                        : Expression.Convert(member.Value, p.ParameterType);
                }

                if (p.HasDefaultValue)
                {
                    return Expression.Convert(Expression.Constant(p.DefaultValue), p.ParameterType);
                }

                // Partial projections can omit constructor arguments that are not selected.
                // We fall back to default values for missing arguments to keep selector
                // construction non-throwing for record-like types.
                return Expression.Default(p.ParameterType);
            }).ToArray();

            return Expression.New(bestMatchingConstructor.Constructor, arguments);
        }

        // Real projection (member-init or a covering constructor) is attempted first even for
        // non-public construction surfaces (a non-public parameterless constructor with
        // non-public setters, or a non-public covering constructor), so this reuse is only
        // reached for EF DI/proxy entities and for types that can neither be constructed nor
        // partially bound. We reuse the source instance as a last resort. Projection only
        // optimizes data fetching; the GraphQL execution layer still shapes the response to
        // the selection set, so reuse is always valid (it may fetch more columns than
        // strictly required).
        return context.Parent;
    }

    // Finds the constructor used to populate every read-only leaf property of a type. The
    // search only depends on the runtime type, not on which properties a given selection
    // happens to select, so every selector built for the same type resolves to the same
    // constructor and merges cleanly with its siblings.
    private static (ConstructorInfo? Constructor, ParameterInfo[] Parameters) FindCoveringConstructor(
        Type type)
    {
        var readOnlyLeafProperties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p is { CanRead: true, CanWrite: false } && IsLeafScalarAutoProperty(p))
            .ToArray();

        return type
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(c => !IsRecordCopyConstructor(c, type))
            .Select(c => (Constructor: c, Parameters: c.GetParameters()))
            .OrderBy(c => c.Parameters.Length)
            .FirstOrDefault(c =>
                readOnlyLeafProperties.All(property =>
                    c.Parameters.Any(p =>
                        string.Equals(property.Name, p.Name, StringComparison.OrdinalIgnoreCase)
                        && property.PropertyType.IsAssignableTo(p.ParameterType)))
                && c.Parameters.All(p =>
                    p.HasDefaultValue || FindMatchingLeafProperty(type, p) is not null));
    }

    // Builds a constructor argument from the matching leaf property of the source instance
    // (never from the value the selection already projected), or from the parameter default
    // value when no property matches. Feeding real source values keeps validating constructors
    // from failing on properties that were not selected.
    private static Expression BuildConstructorArgument(
        Expression parent,
        Type parentType,
        ParameterInfo parameter)
    {
        var property = FindMatchingLeafProperty(parentType, parameter);

        if (property is not null)
        {
            var accessor = Expression.Property(parent, property);
            return property.PropertyType == parameter.ParameterType
                ? accessor
                : Expression.Convert(accessor, parameter.ParameterType);
        }

        return Expression.Convert(Expression.Constant(parameter.DefaultValue), parameter.ParameterType);
    }

    // A constructor parameter is satisfiable by a leaf-scalar auto-property with the same name
    // (case-insensitive) whose type is assignable to the parameter type. Restricting the match
    // to leaf scalars keeps this from feeding a whole unselected navigation, a value object, or
    // a computed property into a constructor.
    private static PropertyInfo? FindMatchingLeafProperty(Type type, ParameterInfo parameter)
        => type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(property =>
                property.CanRead
                && IsLeafScalarAutoProperty(property)
                && string.Equals(property.Name, parameter.Name, StringComparison.OrdinalIgnoreCase)
                && property.PropertyType.IsAssignableTo(parameter.ParameterType));

    private void CollectSelection(
        Context context,
        Selection selection,
        TypeNode parent)
    {
        var field = selection.Field;
        var namedType = field.Type.NamedType();

        // A field is projectable if its resolver reads the underlying member (declared on
        // the parent runtime type, a base type, or an implemented interface) or explicitly
        // replaces that member (ResolveWith / [BindMember]). A pure resolver is deliberately
        // not required: middleware removes the pure resolver, but the resolver still reads
        // the member, so the member still has to be projected.
        var isMemberResolver = field.ResolverMember?.DeclaringType?.IsAssignableFrom(
            field.DeclaringType.RuntimeType) == true;
        var isMemberReplacement = field.Flags.HasFlag(CoreFieldFlags.MemberReplacement);

        if (!isMemberResolver && !isMemberReplacement)
        {
            return;
        }

        // A writable property is always projectable. A read-only property is only projectable
        // when it is a leaf scalar auto-property (a compiler-generated getter backed by real
        // storage) that a covering constructor can populate. This excludes computed properties
        // and read-only navigation or collection properties, which must keep being skipped.
        if (field.Member is not PropertyInfo { CanRead: true } property
            || (!property.CanWrite && !IsProjectableReadOnlyLeaf(namedType, property)))
        {
            return;
        }

        if (field.Flags.HasFlag(CoreFieldFlags.Connection)
            || field.Flags.HasFlag(CoreFieldFlags.CollectionSegment))
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

    // A read-only property is only safe to project when its GraphQL field type is a leaf and
    // it is a real leaf-scalar auto-property. This excludes computed properties, read-only
    // navigation or collection properties, and a read-only navigation bound to a leaf GraphQL
    // field through a member replacement (fluent ResolveWith / [BindMember]), all of which must
    // keep being skipped.
    private static bool IsProjectableReadOnlyLeaf(ITypeDefinition namedType, PropertyInfo property)
        => namedType.IsLeafType() && IsLeafScalarAutoProperty(property);

    // A leaf scalar auto-property is backed by a compiler-generated getter (real storage, for
    // example an EF-mapped column) and has a value-type or string CLR type, so it can be read
    // straight from the source instance or fed into a constructor argument without triggering
    // navigation loads or running computed logic.
    private static bool IsLeafScalarAutoProperty(PropertyInfo property)
        => (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
            && property.GetMethod?.GetCustomAttribute<CompilerGeneratedAttribute>() is not null;

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
            context.Mask.Value |= selection.IncludeConditionMask;

            // This is the only place that checks include flags.
            // If another check is added, its condition bits must be added to the mask too.
            // Otherwise the selector cache may reuse the wrong expression.
            if (!selection.IsIncluded(context.IncludeFlags))
            {
                continue;
            }

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

    private static Expression? BuildMemberValueExpression(
        PropertyNode node,
        Context context)
    {
        var propertyAccessor = Expression.Property(context.Parent, node.Property);

        if (node.Nodes.Count == 0)
        {
            if (IsNullableType(context, node.Property))
            {
                return Expression.Condition(
                    Expression.Equal(propertyAccessor, Expression.Constant(null)),
                    Expression.Constant(null, node.Property.PropertyType),
                    propertyAccessor);
            }

            return propertyAccessor;
        }

        if (TryGetCollectionElementType(node.Property.PropertyType, out var elementType))
        {
            var projectedCollection = BuildCollectionProjectionExpression(context, node, propertyAccessor, elementType);

            if (IsNullableType(context, node.Property))
            {
                return Expression.Condition(
                    Expression.Equal(propertyAccessor, Expression.Constant(null, node.Property.PropertyType)),
                    Expression.Constant(null, node.Property.PropertyType),
                    projectedCollection);
            }

            return projectedCollection;
        }

        var newContext = context with { Parent = propertyAccessor, ParentType = node.Property.PropertyType };
        var nestedExpression = BuildTypeSwitchExpression(newContext, node);

        if (IsNullableType(context, node.Property))
        {
            return Expression.Condition(
                Expression.Equal(propertyAccessor, Expression.Constant(null)),
                Expression.Constant(null, node.Property.PropertyType),
                nestedExpression ?? Expression.Constant(null, node.Property.PropertyType));
        }

        return nestedExpression;
    }

    private static Expression BuildCollectionProjectionExpression(
        Context context,
        PropertyNode node,
        Expression source,
        Type elementType)
    {
        var parameter = Expression.Parameter(elementType, "item");
        var itemContext = context with { Parent = parameter, ParentType = elementType };
        var nestedExpression = BuildTypeSwitchExpression(itemContext, node);

        if (nestedExpression is null)
        {
            throw new InvalidOperationException(
                $"Unable to build projection for collection property '{node.Property.Name}'.");
        }

        var projectedItem = nestedExpression.Type == elementType
            ? nestedExpression
            : Expression.Convert(nestedExpression, elementType);
        var itemProjection = Expression.Lambda(projectedItem, parameter);
        var projectedItems = Expression.Call(
            s_selectMethod.MakeGenericMethod(elementType, elementType),
            source,
            itemProjection);

        return CreateCollectionExpression(projectedItems, node.Property.PropertyType, elementType);
    }

    private static Expression CreateCollectionExpression(
        Expression source,
        Type targetType,
        Type elementType)
    {
        if (targetType.IsArray)
        {
            return Expression.Call(s_toArrayMethod.MakeGenericMethod(elementType), source);
        }

        if (TryCreateSetExpression(source, targetType, elementType, out var setExpression))
        {
            return setExpression!;
        }

        var listExpression = Expression.Call(s_toListMethod.MakeGenericMethod(elementType), source);

        if (targetType.IsAssignableFrom(listExpression.Type))
        {
            return listExpression.Type == targetType
                ? listExpression
                : Expression.Convert(listExpression, targetType);
        }

        var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
        var enumerableCtor = targetType.GetConstructor([enumerableType]);

        if (enumerableCtor is not null)
        {
            return Expression.New(enumerableCtor, source);
        }

        var listType = typeof(List<>).MakeGenericType(elementType);
        var listCtor = targetType.GetConstructor([listType]);

        if (listCtor is not null)
        {
            return Expression.New(listCtor, listExpression);
        }

        throw new NotSupportedException(
            $"Collection projection for property type '{targetType.FullName}' is not supported.");
    }

    private static bool TryCreateSetExpression(
        Expression source,
        Type targetType,
        Type elementType,
        out Expression? expression)
    {
        var setFactoryType = default(Type);

        if (targetType.IsGenericType)
        {
            var typeDefinition = targetType.GetGenericTypeDefinition();
            if (typeDefinition == typeof(ISet<>)
                || typeDefinition == typeof(HashSet<>))
            {
                setFactoryType = typeof(HashSet<>).MakeGenericType(elementType);
            }
            else if (typeDefinition == typeof(SortedSet<>))
            {
                setFactoryType = typeof(SortedSet<>).MakeGenericType(elementType);
            }
        }

        if (setFactoryType is not null)
        {
            var constructor = setFactoryType.GetConstructor([source.Type]);

            if (constructor is null)
            {
                expression = null;
                return false;
            }

            var newSet = Expression.New(constructor, source);
            expression = newSet.Type == targetType
                ? newSet
                : Expression.Convert(newSet, targetType);
            return true;
        }

        expression = null;
        return false;
    }

    private static bool TryGetCollectionElementType(
        Type type,
        out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (type.IsGenericType && IsSupportedCollectionDefinition(type.GetGenericTypeDefinition()))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType
                && IsSupportedCollectionDefinition(interfaceType.GetGenericTypeDefinition()))
            {
                elementType = interfaceType.GetGenericArguments()[0];
                return true;
            }
        }

        elementType = default!;
        return false;
    }

    private static bool IsSupportedCollectionDefinition(Type typeDefinition)
        => typeDefinition == typeof(IEnumerable<>)
            || typeDefinition == typeof(IReadOnlyCollection<>)
            || typeDefinition == typeof(IReadOnlyList<>)
            || typeDefinition == typeof(ICollection<>)
            || typeDefinition == typeof(IList<>)
            || typeDefinition == typeof(ISet<>)
            || typeDefinition == typeof(List<>)
            || typeDefinition == typeof(HashSet<>)
            || typeDefinition == typeof(SortedSet<>);

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
        NullabilityInfoContext NullabilityInfoContext,
        ulong IncludeFlags,
        DependencyMask Mask)
    {
        public TypeNode? GetRequirements(Selection selection)
        {
            var flags = selection.Field.Flags;
            return (flags & CoreFieldFlags.WithRequirements) == CoreFieldFlags.WithRequirements
                ? Requirements.GetRequirements(selection.Field)
                : null;
        }
    }

    private sealed class DependencyMask
    {
        public ulong Value;
    }

    private static bool ShouldReuseExistingInstance(Type type)
        => type.GetConstructor(Type.EmptyTypes) is not null
            && type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Any(t =>
                    t.GetParameters().Length > 0
                    && !IsRecordCopyConstructor(t, type));

    private static bool IsRecordCopyConstructor(ConstructorInfo constructor, Type declaringType)
    {
        var parameters = constructor.GetParameters();

        return parameters.Length == 1
            && parameters[0].ParameterType == declaringType;
    }
}
