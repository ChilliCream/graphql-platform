using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// This rules allows to limit cycles across unique field coordinates.
/// </summary>
/// <param name="coordinateCycleLimits">
/// Specifies specific coordinate cycle limits.
/// </param>
/// <param name="defaultCycleLimit">
/// Specifies the default coordinate cycle limit.
/// </param>
internal sealed class MaxAllowedFieldCycleDepthVisitor(
    ImmutableArray<(SchemaCoordinate Coordinate, ushort MaxAllowed)> coordinateCycleLimits,
    ushort? defaultCycleLimit)
    : TypeDocumentValidatorVisitor
{
    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        DocumentValidatorContext context)
    {
        context.InitializeFieldDepth(coordinateCycleLimits, defaultCycleLimit);
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
            // we are ignoring introspection fields in this visitor.
            if (of.IsIntrospectionField)
            {
                return Skip;
            }

            if (!context.FieldDepth().Add(of.Coordinate))
            {
                context.ReportMaxCoordinateCycleDepthOverflow(node);
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
        IEnumerable<(SchemaCoordinate Coordinate, ushort MaxAllowed)> limits,
        ushort? defaultLimit)
    {
        var feature = context.Features.GetOrSet<FieldDepthCycleTracker>();
        feature.Initialize(limits, defaultLimit);
    }

    public static FieldDepthCycleTracker FieldDepth(this DocumentValidatorContext context)
    {
        return context.Features.GetRequired<FieldDepthCycleTracker>();
    }
}
