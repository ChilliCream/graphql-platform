using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Validation.Options;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// This rules ensures that recursive introspection fields cannot be used
/// to create endless cycles.
/// </summary>
internal sealed class IntrospectionDepthVisitor(
    IIntrospectionOptionsAccessor options)
    : TypeDocumentValidatorVisitor
{
    private readonly (SchemaCoordinate Coordinate, ushort MaxAllowed)[] _limits =
    [
        (new SchemaCoordinate("__Type", "fields"), options.MaxAllowedListRecursiveDepth),
        (new SchemaCoordinate("__Type", "inputFields"), options.MaxAllowedListRecursiveDepth),
        (new SchemaCoordinate("__Type", "interfaces"), options.MaxAllowedListRecursiveDepth),
        (new SchemaCoordinate("__Type", "possibleTypes"), options.MaxAllowedListRecursiveDepth),
        (new SchemaCoordinate("__Type", "ofType"), options.MaxAllowedOfTypeDepth)
    ];

    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        DocumentValidatorContext context)
    {
        context.InitializeFieldDepth(_limits);
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        if (IntrospectionFieldNames.TypeName.Equals(node.Name.Value, StringComparison.Ordinal))
        {
            return Skip;
        }

        if (context.Types.TryPeek(out var type)
            && type.NamedType() is IComplexTypeDefinition ot
            && ot.Fields.TryGetField(node.Name.Value, out var of))
        {
            // we are only interested in fields if the root field is either
            // __type or __schema.
            if (context.OutputFields.Count == 0
                && !of.IsIntrospectionField)
            {
                return Skip;
            }

            if (!context.FieldDepth().Add(of.Coordinate))
            {
                context.ReportMaxIntrospectionDepthOverflow(node);
                return Break;
            }

            context.OutputFields.Push(of);
            context.Types.Push(of.Type);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        DocumentValidatorContext context)
    {
        context.FieldDepth().Remove(context.OutputFields.Peek().Coordinate);
        context.Types.Pop();
        context.OutputFields.Pop();
        return Continue;
    }
}

file static class ContextExtensions
{
    public static void InitializeFieldDepth(
        this DocumentValidatorContext context,
        (SchemaCoordinate Coordinate, ushort MaxAllowed)[] limits)
    {
        var feature = context.Features.GetOrSet<FieldDepthCycleTracker>();
        feature.Initialize(limits);
    }

    public static FieldDepthCycleTracker FieldDepth(this DocumentValidatorContext context)
    {
        return context.Features.GetRequired<FieldDepthCycleTracker>();
    }
}
