using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Validation.Options;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Validation.Rules;

internal sealed class MaxExecutionDepthVisitor(
    IMaxExecutionDepthOptionsAccessor options)
    : DocumentValidatorVisitor
{
    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        IDocumentValidatorContext context)
    {
        // if the depth analysis was skipped for this request we will just
        // stop traversing the graph.
        if (context.ContextData.ContainsKey(SkipDepthAnalysis))
        {
            return Break;
        }

        // if we have a request override we will pick it over the configured value.
        if (context.ContextData.TryGetValue(MaxAllowedExecutionDepth, out var value) &&
            value is int maxAllowedDepth)
        {
            context.Allowed = maxAllowedDepth;
        }

        // otherwise we will go with the configured value
        else if(options.MaxAllowedExecutionDepth.HasValue)
        {
            context.Allowed = options.MaxAllowedExecutionDepth.Value;
        }

        // if there is no configured value we will just stop traversing the graph
        else
        {
            return Break;
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        context.Count = 0;
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        context.Max = context.Count > context.Max
            ? context.Count
            : context.Max;

        if (context.Allowed < context.Max)
        {
            context.ReportError(
                context.MaxExecutionDepth(
                    node,
                    context.Allowed,
                    context.Max));
            return Break;
        }

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        if (options.SkipIntrospectionFields &&
            node.Name.Value.StartsWith("__"))
        {
            return Skip;
        }

        context.Fields.Push(node);

        if (context.Count < context.Fields.Count)
        {
            context.Count = context.Fields.Count;
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        context.Fields.Pop();
        return Continue;
    }
}
