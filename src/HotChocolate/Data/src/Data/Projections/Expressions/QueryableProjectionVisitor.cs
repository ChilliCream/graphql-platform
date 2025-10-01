using System.Linq.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Requirements;
using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Projections.Expressions;

public class QueryableProjectionVisitor : ProjectionVisitor<QueryableProjectionContext>
{
    protected override ISelectionVisitorAction VisitObjectType(
        IOutputFieldDefinition field,
        ObjectType objectType,
        ISelection selection,
        QueryableProjectionContext context)
    {
        var isAbstractType = field.Type.NamedType().IsAbstractType();

        if (!context.TryGetQueryableScope(out var scope))
        {
            return base.VisitObjectType(field, objectType, selection, context);
        }

        // Collect requirements for all types (abstract and non-abstract)
        var selections = context.ResolverContext.GetSelections(objectType, selection, true);
        CollectRequirements(selections, context, scope);

        if (!isAbstractType)
        {
            return base.VisitObjectType(field, objectType, selection, context);
        }

        if (selections.Count == 0)
        {
            return Continue;
        }

        context.PushInstance(Expression.Convert(context.GetInstance(), objectType.RuntimeType));
        scope.Level.Push(new Queue<MemberAssignment>());

        var res = base.VisitObjectType(field, objectType, selection, context);

        context.PopInstance();
        scope.AddAbstractType(objectType.RuntimeType, scope.Level.Pop());

        return res;
    }

    private static void CollectRequirements(
        IReadOnlyList<ISelection> selections,
        QueryableProjectionContext context,
        QueryableProjectionScope scope)
    {
        if (!context.ResolverContext.Schema.Features.TryGet<FieldRequirementsMetadata>(out var requirements))
        {
            return;
        }

        foreach (var selection in selections)
        {
            var flags = selection.Field.Flags;
            if ((flags & CoreFieldFlags.WithRequirements) == CoreFieldFlags.WithRequirements)
            {
                var typeNode = requirements.GetRequirements(selection.Field);
                if (typeNode is not null)
                {
                    CollectRequiredProperties(typeNode, context, scope);
                }
            }
        }
    }

    private static void CollectRequiredProperties(
        TypeNode typeNode,
        QueryableProjectionContext context,
        QueryableProjectionScope scope)
    {
        foreach (var propertyNode in typeNode.Nodes)
        {
            CollectRequiredProperty(propertyNode, context, scope);
        }
    }

    private static void CollectRequiredProperty(
        PropertyNode propertyNode,
        QueryableProjectionContext context,
        QueryableProjectionScope scope)
    {
        var property = propertyNode.Property;
        var instance = context.GetInstance();

        // Check if already projected to avoid duplicates
        foreach (var assignment in scope.Level.Peek())
        {
            if (assignment.Member == property)
            {
                return;
            }
        }

        var propertyAccess = Expression.Property(instance, property);
        var binding = Expression.Bind(property, propertyAccess);
        scope.Level.Peek().Enqueue(binding);
    }

    public static readonly QueryableProjectionVisitor Default = new();
}
