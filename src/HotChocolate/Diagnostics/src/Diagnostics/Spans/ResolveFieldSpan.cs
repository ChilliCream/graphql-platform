using System.Diagnostics;
using HotChocolate.Resolvers;
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

        activity.SetTag(GraphQL.Field.Alias, selection.ResponseName);
        activity.SetTag(GraphQL.Field.Path, context.Path.Print());
        activity.SetTag(GraphQL.Field.Name, coordinate.MemberName);
        activity.SetTag(GraphQL.Field.Coordinate, activity.DisplayName);
        activity.SetTag(GraphQL.Field.ParentType, coordinate.Name);

        return new ResolveFieldSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
        if (Activity.Status != ActivityStatusCode.Error)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichResolveFieldValue(context, Activity);
    }
}
