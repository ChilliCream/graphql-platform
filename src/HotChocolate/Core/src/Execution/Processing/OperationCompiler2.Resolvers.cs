using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationCompiler2
{
    internal static FieldDelegate CreateFieldPipeline(
        ISchema schema,
        IObjectField field,
        FieldNode selection,
        HashSet<string> processed,
        List<FieldMiddleware> pipelineComponents)
    {
        var pipeline = field.Middleware;

        if (selection.Directives.Count == 0)
        {
            return pipeline;
        }

        // if we have selection directives we will inspect them and try to build a
        // pipeline from them if they have middleware components.
        BuildDirectivePipeline(schema, selection, processed, pipelineComponents);

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

        // at last, we clear the rented lists
        processed.Clear();
        pipelineComponents.Clear();

        return pipeline;
    }

    private static PureFieldDelegate? TryCreatePureField(
        ISchema schema,
        IObjectField field,
        FieldNode selection)
    {
        if (field.PureResolver is not null && selection.Directives.Count == 0)
        {
            return field.PureResolver;
        }

        for (var i = 0; i < selection.Directives.Count; i++)
        {
            if (schema.TryGetDirectiveType(selection.Directives[i].Name.Value, out var type) &&
                type.Middleware is not null)
            {
                return null;
            }
        }

        return field.PureResolver;
    }

    private static void BuildDirectivePipeline(
        ISchema schema,
        FieldNode selection,
        HashSet<string> processed,
        List<FieldMiddleware> pipelineComponents)
    {
        for (var i = 0; i < selection.Directives.Count; i++)
        {
            var directiveNode = selection.Directives[i];
            if (schema.TryGetDirectiveType(directiveNode.Name.Value, out var directiveType)
                && directiveType.Middleware is not null
                && (directiveType.IsRepeatable || processed.Add(directiveType.Name)))
            {
                var directive = new Directive(
                    directiveType,
                    directiveNode,
                    directiveType.Parse(directiveNode));
                var directiveMiddleware = directiveType.Middleware;
                pipelineComponents.Add(next => directiveMiddleware(next, directive));
            }
        }
    }
}
