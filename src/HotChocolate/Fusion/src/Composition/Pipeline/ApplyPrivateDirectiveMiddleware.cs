using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class ApplyPrivateDirectiveMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var rewriterContext = new PrivateContext(context.FusionTypes.Private.Name);
        var collectPrivateFields = new CollectPrivateFieldsVisitor();

        foreach (var subgraph in context.Subgraphs)
        {
            collectPrivateFields.VisitSchema(subgraph, rewriterContext);
            
            RemovePrivateFields(
                context.FusionTypes,
                context.FusionGraph,
                subgraph.Name,
                rewriterContext.PrivateFieldCoordinates);

            rewriterContext.PrivateFieldCoordinates.Clear();
        }

        if (!context.Log.HasErrors)
        {
            await next(context);
        }
    }

    private static void RemovePrivateFields(
        FusionTypes types,
        Schema fusionGraph,
        string subgraphName,
        List<SchemaCoordinate> coordinates)
    {
        const string subgraphArg = "subgraph";
        var source = types.Source.Name;
        var resolver = types.Resolver.Name;

        foreach (var group in coordinates.GroupBy(t => t.Name))
        {
            if (!fusionGraph.Types.TryGetType<ComplexType>(group.Key, out var type))
            {
                continue;
            }

            foreach (var coordinate in group)
            {
                if (!type.Fields.TryGetField(coordinate.MemberName!, out var field))
                {
                    continue;
                }

                var match = false;
                short moreThanOne = 0;
                Directive? matchingDir = null;

                foreach (var directive in field.Directives[source])
                {
                    if (moreThanOne < 2)
                    {
                        moreThanOne++;
                    }

                    if (!match && ((StringValueNode)directive.Arguments[subgraphArg]).Value.EqualsOrdinal(subgraphName))
                    {
                        matchingDir = directive;
                        match = true;
                    }

                    if (match && moreThanOne > 1)
                    {
                        break;
                    }
                }

                if (!match)
                {
                    match = false;
                    moreThanOne = 0;
                    matchingDir = null;
                    
                    foreach (var directive in field.Directives[resolver])
                    {
                        if (moreThanOne < 2)
                        {
                            moreThanOne++;
                        }

                        if (!match && ((StringValueNode)directive.Arguments[subgraphArg]).Value.EqualsOrdinal(subgraphName))
                        {
                            matchingDir = directive;
                            match = true;
                        }

                        if (match && moreThanOne > 1)
                        {
                            break;
                        }
                    }
                }

                if (matchingDir is not null)
                {
                    field.Directives.Remove(matchingDir);
                }

                if (match && moreThanOne == 1)
                {
                    type.Fields.Remove(field);
                }
            }
        }
    }

    private class CollectPrivateFieldsVisitor : SchemaVisitor<PrivateContext>
    {
        public override void VisitObjectType(ObjectType type, PrivateContext context)
        {
            base.VisitObjectType(type, context);

            if (context.PrivateFields.Count <= 0)
            {
                return;
            }

            foreach (var field in context.PrivateFields)
            {
                context.PrivateFieldCoordinates.Add(new SchemaCoordinate(type.Name, field.Name));
            }

            context.PrivateFields.Clear();
        }

        public override void VisitInterfaceType(InterfaceType type, PrivateContext context)
        {
            base.VisitInterfaceType(type, context);

            if (context.PrivateFields.Count <= 0)
            {
                return;
            }

            foreach (var field in context.PrivateFields)
            {
                context.PrivateFieldCoordinates.Add(new SchemaCoordinate(type.Name, field.Name));
            }

            context.PrivateFields.Clear();
        }

        public override void VisitOutputField(OutputField field, PrivateContext context)
        {
            if (field.Directives.ContainsName(context.PrivateDirectiveName))
            {
                context.PrivateFields.Add(field);
            }

            base.VisitOutputField(field, context);
        }
    }

    private sealed class PrivateContext(string privateDirectiveName)
    {
        public readonly string PrivateDirectiveName = privateDirectiveName;

        public readonly List<OutputField> PrivateFields = new();

        public readonly List<SchemaCoordinate> PrivateFieldCoordinates = new();
    }
}