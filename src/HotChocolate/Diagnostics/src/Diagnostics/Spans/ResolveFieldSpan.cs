using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class ResolveFieldSpan(
    Activity activity,
    IMiddlewareContext context,
    ActivityEnricher enricher) : SpanBase(activity)
{
    public static ResolveFieldSpan? Start(
        ActivitySource source,
        IMiddlewareContext context,
        ActivityEnricher enricher)
    {
        var selection = context.Selection;
        var coordinate = selection.Field.Coordinate;

        var activity = source.StartActivity(coordinate.ToString());

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Resolve);

        activity.SetTag(GraphQL.Selection.Name, selection.ResponseName);
        activity.SetTag(GraphQL.Selection.Path, context.Path.Print());
        activity.SetTag(GraphQL.Selection.Field.Name, coordinate.MemberName);
        activity.SetTag(GraphQL.Selection.Field.Coordinate, activity.DisplayName);
        activity.SetTag(GraphQL.Selection.Field.ParentType, coordinate.Name);

        // TODO: Re-add this
        // context.SetLocalState(ResolverActivity, activity);

        return new ResolveFieldSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
        Activity.MarkAsSuccess();

        enricher.EnrichResolveFieldValue(Activity, context);
    }
}
