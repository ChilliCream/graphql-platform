using HotChocolate.Execution;
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
        var requestOverrides = context.GetRequestOverrides();

        // if the depth analysis was skipped for this request, we will just
        // stop traversing the graph.
        if (requestOverrides?.SkipValidation ?? false)
        {
            return Break;
        }

        var maxAllowedDepth = requestOverrides?.MaxAllowedDepth ?? options.MaxAllowedExecutionDepth;

        if (maxAllowedDepth.HasValue)
        {
            context.SetMaxExecutionDepth(maxAllowedDepth.Value);
            return base.Enter(node, context);
        }

        return Break;
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
        if (!context.IsMaxPathLengthAllowed())
        {
            context.ReportError(context.MaxExecutionDepthError(node));
            return Break;
        }

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        if (options.SkipIntrospectionFields
            && node.Name.Value.StartsWith("__"))
        {
            return Skip;
        }

        context.Fields.Push(node);
        context.CapturePathLength();

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        DocumentValidatorContext context)
    {
        context.Fields.Pop();
        return Continue;
    }
}

file sealed class MaxExecutionDepthVisitorFeature : ValidatorFeature
{
    public int Allowed { get; set; }

    public int Max { get; set; }

    public int Count { get; set; }

    protected internal override void Reset()
    {
        Allowed = 0;
        Max = 0;
    }
}

file static class MaxExecutionDepthRequestFeatureExtensions
{
    public static void SetMaxExecutionDepth(
        this DocumentValidatorContext context,
        int maxAllowedDepth)
    {
        context.Features.GetOrSet<MaxExecutionDepthVisitorFeature>().Allowed = maxAllowedDepth;
    }

    public static bool IsMaxPathLengthAllowed(
        this DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<MaxExecutionDepthVisitorFeature>();
        feature.Max = feature.Count > feature.Max ? feature.Count : feature.Max;
        return feature.Allowed >= feature.Max;
    }

    public static void CapturePathLength(
        this DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<MaxExecutionDepthVisitorFeature>();

        if (feature.Count < context.Fields.Count)
        {
            feature.Count = context.Fields.Count;
        }
    }

    public static IError MaxExecutionDepthError(
        this DocumentValidatorContext context,
        OperationDefinitionNode node)
    {
        var feature = context.Features.GetRequired<MaxExecutionDepthVisitorFeature>();
        return context.MaxExecutionDepth(node, feature.Allowed, feature.Max);
    }

    public static MaxExecutionDepthRequestOverrides? GetRequestOverrides(
        this DocumentValidatorContext context)
        => context.Features.Get<MaxExecutionDepthRequestOverrides>();
}
