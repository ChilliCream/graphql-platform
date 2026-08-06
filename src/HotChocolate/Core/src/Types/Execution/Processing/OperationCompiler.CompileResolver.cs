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
                var directiveMiddleware = directiveType.Middleware;

                if (ContainsVariable(directiveNode))
                {
                    pipelineComponents.Add(
                        next => context =>
                        {
                            var rewrittenDirectiveNode =
                                RewriteDirectiveNode(directiveType, directiveNode, context.Variables);

                            var directive = CreateDirective(
                                directiveType,
                                rewrittenDirectiveNode,
                                directiveNode);

                            return directiveMiddleware(next, directive)(context);
                        });
                }
                else
                {
                    var directive = CreateDirective(directiveType, directiveNode);
                    pipelineComponents.Add(next => directiveMiddleware(next, directive));
                }
            }
        }
    }

    private static Directive CreateDirective(
        DirectiveType directiveType,
        DirectiveNode directiveNode,
        DirectiveNode? errorLocationNode = null)
    {
        try
        {
            return new Directive(
                directiveType,
                directiveNode,
                directiveType.Parse(directiveNode));
        }
        catch (LeafCoercionException ex)
        {
            throw new LeafCoercionException(
                ErrorBuilder.FromError(ex.Errors[0])
                    .TryAddLocation(errorLocationNode ?? directiveNode)
                    .Build(),
                ex.Type);
        }
    }

    private static DirectiveNode RewriteDirectiveNode(
        DirectiveType directiveType,
        DirectiveNode directiveNode,
        IVariableValueCollection variableValues)
    {
        if (directiveNode.Arguments.Count == 0)
        {
            return directiveNode;
        }

        var hasChanges = false;
        var rewrittenArguments = new ArgumentNode[directiveNode.Arguments.Count];

        for (var i = 0; i < directiveNode.Arguments.Count; i++)
        {
            var argumentNode = directiveNode.Arguments[i];
            var rewrittenValue = argumentNode.Value;

            if (directiveType.Arguments.TryGetField(argumentNode.Name.Value, out var argument))
            {
                rewrittenValue = VariableRewriter.Rewrite(
                    argumentNode.Value,
                    argument.Type,
                    argument.DefaultValue,
                    variableValues);
            }

            if (!ReferenceEquals(rewrittenValue, argumentNode.Value))
            {
                rewrittenArguments[i] = argumentNode.WithValue(rewrittenValue);
                hasChanges = true;
            }
            else
            {
                rewrittenArguments[i] = argumentNode;
            }
        }

        return hasChanges
            ? directiveNode.WithArguments(rewrittenArguments)
            : directiveNode;
    }

    private static bool ContainsVariable(DirectiveNode directiveNode)
    {
        for (var i = 0; i < directiveNode.Arguments.Count; i++)
        {
            if (ContainsVariable(directiveNode.Arguments[i].Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsVariable(IValueNode value)
    {
        switch (value.Kind)
        {
            case SyntaxKind.Variable:
                return true;

            case SyntaxKind.ListValue:
                var list = (ListValueNode)value;
                for (var i = 0; i < list.Items.Count; i++)
                {
                    if (ContainsVariable(list.Items[i]))
                    {
                        return true;
                    }
                }
                break;

            case SyntaxKind.ObjectValue:
                var obj = (ObjectValueNode)value;
                for (var i = 0; i < obj.Fields.Count; i++)
                {
                    if (ContainsVariable(obj.Fields[i].Value))
                    {
                        return true;
                    }
                }
                break;
        }

        return false;
    }
}
