#if NET6_0_OR_GREATER
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
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

public sealed class PropertyNode(
    PropertyInfo property,
    ImmutableArray<PropertyNode> nodes)
{
    public PropertyInfo Property { get; } = property;

    public ImmutableArray<PropertyNode> Nodes { get; } = nodes;
}

internal sealed class FieldRequirementsMetadata
{
    private readonly Dictionary<SchemaCoordinate,  ImmutableArray<PropertyNode>> _allRequirements = new();
    private bool _sealed;

    public ImmutableArray<PropertyNode>? GetRequirements(IObjectField field)
        => _allRequirements.TryGetValue(field.Coordinate, out var requirements) ? requirements : null;

    public bool HasRequirements(SchemaCoordinate fieldCoordinate)
        => _allRequirements.ContainsKey(fieldCoordinate);

    public void TryAddRequirements(SchemaCoordinate fieldCoordinate, ImmutableArray<PropertyNode> requirements)
    {
        if(_sealed)
        {
            throw new InvalidOperationException("The requirements are sealed.");
        }

        _allRequirements.TryAdd(fieldCoordinate, requirements);
    }

    public void Seal()
        => _sealed = true;
}

internal static class PropertyTreeBuilder
{
    public static ImmutableArray<PropertyNode> Build(
        SchemaCoordinate fieldCoordinate,
        Type type,
        string requirements)
    {
        if (!requirements.Trim().StartsWith("{"))
        {
            requirements = "{" + requirements + "}";
        }

        var selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(requirements);
        return Build(fieldCoordinate, type, selectionSet, Path.Root);
    }

    private static ImmutableArray<PropertyNode> Build(
        SchemaCoordinate fieldCoordinate,
        Type type,
        SelectionSetNode selectionSet,
        Path path)
    {
        var builder = ImmutableArray.CreateBuilder<PropertyNode>();

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode field)
            {
                if(field.Arguments.Count > 0)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage("Field arguments in the requirements syntax.")
                            .Build());
                }

                if(field.Directives.Count > 0)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage("Field directives in the requirements syntax.")
                            .Build());
                }

                if (field.Alias is not null)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage("Field aliases in the requirements syntax.")
                            .Build());

                }

                var fieldPath = path.Append(field.Name.Value);
                var property = type.GetProperty(field.Name.Value);

                if(property is null)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage(
                                "The field requirement `{0}` does not exist on `{1}`.",
                                fieldPath.ToString(),
                                fieldCoordinate.ToString())
                            .Build());
                }

                var nodes =
                    field.SelectionSet is not null
                        ? Build(fieldCoordinate, property.PropertyType, field.SelectionSet, fieldPath)
                        : ImmutableArray<PropertyNode>.Empty;

                builder.Add(new PropertyNode(property, nodes));
            }
            else
            {
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage("Only field selections are allowed in the requirements syntax.")
                        .Build());
            }
        }

        return builder.ToImmutable();
    }
}

internal sealed class RequirementsTypeInterceptor : TypeInterceptor
{
    private readonly FieldRequirementsMetadata _metadata = new();

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is SchemaTypeDefinition schema)
        {
            schema.Features.Set(_metadata);
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is not ObjectTypeDefinition typeDef)
        {
            return;
        }

        var runtimeType = typeDef.RuntimeType != typeof(object) ? typeDef.RuntimeType : null;

        foreach (var fieldDef in typeDef.Fields)
        {
            if((fieldDef.Flags & FieldFlags.WithRequirements) == FieldFlags.WithRequirements)
            {
                var fieldCoordinate = new SchemaCoordinate(
                    typeDef.Name,
                    fieldDef.Name);

                // if the source generator already compiled the
                // requirements we will skip it.
                if (_metadata.HasRequirements(fieldCoordinate))
                {
                    continue;
                }

                var requirements = (string)fieldDef.ContextData[WellKnownContextData.FieldRequirementsSyntax]!;
                var entityType = runtimeType ?? (Type)fieldDef.ContextData[WellKnownContextData.FieldRequirementsEntity]!;

                var propertyNodes = PropertyTreeBuilder.Build(
                    fieldCoordinate,
                    entityType,
                    requirements);

                _metadata.TryAddRequirements(fieldCoordinate, propertyNodes);
            }
        }
    }

    internal override void OnAfterCreateSchemaInternal(
        IDescriptorContext context,
        ISchema schema)
        => _metadata.Seal();
}
#endif
