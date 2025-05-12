using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Validation.Options;

namespace HotChocolate.Validation.Rules;

internal sealed class MaxExecutionDepthVisitor(
    IMaxExecutionDepthOptionsAccessor options)
    : DocumentValidatorVisitor
{
    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        DocumentValidatorContext context)
    {
        var feature = context.Features.GetOrSet<MaxExecutionDepthVisitorFeature>();

        // if the depth analysis was skipped for this request, we will just
        // stop traversing the graph.
        if (context.ContextData.ContainsKey(ExecutionContextData.SkipDepthAnalysis))
        {
            return Break;
        }

        // if we have a request override, we will pick it over the configured value.
        if (context.ContextData.TryGetValue(ExecutionContextData.MaxAllowedExecutionDepth, out var value) &&
            value is int maxAllowedDepth)
        {
            feature.Allowed = maxAllowedDepth;
        }

        // otherwise we will go with the configured value
        else if(options.MaxAllowedExecutionDepth.HasValue)
        {
            feature.Allowed = options.MaxAllowedExecutionDepth.Value;
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
        DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<MaxExecutionDepthVisitorFeature>();
        feature.Count = 0;
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        OperationDefinitionNode node,
        DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<MaxExecutionDepthVisitorFeature>();
        feature.Max = feature.Count > feature.Max ? feature.Count : feature.Max;

        if (feature.Allowed < feature.Max)
        {
            context.ReportError(
                context.MaxExecutionDepth(
                    node,
                    feature.Allowed,
                    feature.Max));
            return Break;
        }

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<MaxExecutionDepthVisitorFeature>();

        if (options.SkipIntrospectionFields &&
            node.Name.Value.StartsWith("__"))
        {
            return Skip;
        }

        context.Fields.Push(node);

        if (feature.Count < context.Fields.Count)
        {
            feature.Count = context.Fields.Count;
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        DocumentValidatorContext context)
    {
        context.Fields.Pop();
        return Continue;
    }

    private sealed class MaxExecutionDepthVisitorFeature : ValidatorFeature
    {
        public int Allowed { get; set; }

        public int Max { get; set; }

        public int Count { get; set; }

        public override void Reset()
        {
            Allowed = 0;
            Max = 0;
        }
    }
}
