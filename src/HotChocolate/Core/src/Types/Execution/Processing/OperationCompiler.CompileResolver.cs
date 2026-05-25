using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    internal static FieldDelegate CreateFieldPipeline(Schema schema, ObjectField field, FieldNode selection)
    {
        var pipeline = field.Middleware;

        if (selection.Directives.Count == 0)
        {
            return pipeline;
        }

        var pipelineComponents = new List<FieldMiddleware>();

        // if we have selection directives we will inspect them and try to build a
        // pipeline from them if they have middleware components.
        BuildDirectivePipeline(schema, selection, pipelineComponents);

        // if we found middleware components on the selection directives we will build a new
        // pipeline.
        if (pipelineComponents.Count > 0)
        {
            var next = pipeline;

            for (var i = pipelineComponents.Count - 1; i >= 0; i--)
            {
                next = pipelineComponents[i](next);
            }

            pipeline = next;
        }

        return pipeline;
    }

    private static PureFieldDelegate? TryCreatePureField(
        Schema schema,
        ObjectField field,
        FieldNode selection)
    {
        if (field.PureResolver is not null && selection.Directives.Count == 0)
        {
            return field.PureResolver;
        }

        for (var i = 0; i < selection.Directives.Count; i++)
        {
            if (schema.DirectiveTypes.TryGetDirective(selection.Directives[i].Name.Value, out var type)
                && type.Middleware is not null)
            {
                return null;
            }
        }

        return field.PureResolver;
    }

    // TODO : this needs a rewrite, we need to discuss how we handle field merging with directives
    private static void BuildDirectivePipeline(
        Schema schema,
        FieldNode selection,
        List<FieldMiddleware> pipelineComponents)
    {
        for (var i = 0; i < selection.Directives.Count; i++)
        {
            var directiveNode = selection.Directives[i];
            if (schema.DirectiveTypes.TryGetDirective(directiveNode.Name.Value, out var directiveType)
                && directiveType.Middleware is not null)
            {
                Directive directive;
                try
                {
                    directive = new Directive(
                        directiveType,
                        directiveNode,
                        directiveType.Parse(directiveNode));
                }
                catch (LeafCoercionException ex)
                {
                    throw new LeafCoercionException(
                        ErrorBuilder.FromError(ex.Errors[0])
                            .TryAddLocation(directiveNode)
                            .Build(),
                        ex.Type);
                }

                var directiveMiddleware = directiveType.Middleware;
                pipelineComponents.Add(next => directiveMiddleware(next, directive));
            }
        }
    }
}
