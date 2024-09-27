using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// This rules ensures that recursive introspection fields cannot be used
/// to create endless cycles.
/// </summary>
internal sealed class IntrospectionDepthVisitor : TypeDocumentValidatorVisitor
{
    private readonly (SchemaCoordinate Coordinate, ushort MaxAllowed)[] _limits =
    [
        (new SchemaCoordinate("__Type", "fields"), 1),
        (new SchemaCoordinate("__Type", "inputFields"), 1),
        (new SchemaCoordinate("__Type", "interfaces"), 1),
        (new SchemaCoordinate("__Type", "possibleTypes"), 1),
        (new SchemaCoordinate("__Type", "ofType"), 8)
    ];

    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        IDocumentValidatorContext context)
    {
        context.FieldDepth.Initialize(_limits);
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        if (IntrospectionFields.TypeName.EqualsOrdinal(node.Name.Value))
        {
            return Skip;
        }

        if (context.Types.TryPeek(out var type)
            && type.NamedType() is IComplexOutputType ot
            && ot.Fields.TryGetField(node.Name.Value, out var of))
        {
            // we are only interested in fields if the root field is either
            // __type or __schema.
            if (context.OutputFields.Count == 0
                && !of.IsIntrospectionField)
            {
                return Skip;
            }

            if (!context.FieldDepth.Add(of.Coordinate))
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
        IDocumentValidatorContext context)
    {
        context.FieldDepth.Remove(context.OutputFields.Peek().Coordinate);
        context.Types.Pop();
        context.OutputFields.Pop();
        return Continue;
    }
}
