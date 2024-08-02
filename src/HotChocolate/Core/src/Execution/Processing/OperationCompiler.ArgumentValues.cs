using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static System.StringComparer;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    private ArgumentMap? CoerceArgumentValues(
        ObjectField field,
        FieldNode selection,
        string responseName)
    {
        if (field.Arguments.Count == 0)
        {
            return null;
        }

        var arguments = new Dictionary<string, ArgumentValue>(Ordinal);

        for (var i = 0; i < selection.Arguments.Count; i++)
        {
            var argumentValue = selection.Arguments[i];
            if (field.Arguments.TryGetField(
                argumentValue.Name.Value,
                out var argument))
            {
                arguments[argument.Name] =
                    CreateArgumentValue(
                        responseName,
                        argument,
                        argumentValue,
                        argumentValue.Value,
                        false);
            }
        }

        for (var i = 0; i < field.Arguments.Count; i++)
        {
            var argument = field.Arguments[i];
            if (!arguments.ContainsKey(argument.Name))
            {
                arguments[argument.Name] =
                    CreateArgumentValue(
                        responseName,
                        argument,
                        null,
                        argument.DefaultValue ?? NullValueNode.Default,
                        true);
            }
        }

        return new ArgumentMap(arguments);
    }

    private ArgumentValue CreateArgumentValue(
        string responseName,
        IInputField argument,
        ArgumentNode? argumentValue,
        IValueNode value,
        bool isDefaultValue)
    {
        var validationResult =
            ArgumentNonNullValidator.Validate(
                argument,
                value,
                Path.Root.Append(argument.Name));

        if (argumentValue is not null && validationResult.HasErrors)
        {
            return new ArgumentValue(
                argument,
                ErrorHelper.ArgumentNonNullError(
                    argumentValue,
                    responseName,
                    validationResult));
        }

        if (argument.Type.IsLeafType() && CanBeCompiled(value))
        {
            try
            {
                return new ArgumentValue(
                    argument,
                    value.GetValueKind(),
                    true,
                    isDefaultValue,
                    _parser.ParseLiteral(value, argument),
                    value);
            }
            catch (SerializationException ex)
            {
                if (argumentValue is not null)
                {
                    return new ArgumentValue(
                        argument,
                        ErrorHelper.ArgumentValueIsInvalid(argumentValue, responseName, ex));
                }

                return new ArgumentValue(
                    argument,
                    ErrorHelper.ArgumentDefaultValueIsInvalid(responseName, ex));
            }
        }

        return new ArgumentValue(
            argument,
            value.GetValueKind(),
            false,
            isDefaultValue,
            null,
            value);
    }

    private static bool CanBeCompiled(IValueNode valueLiteral)
    {
        switch (valueLiteral.Kind)
        {
            case SyntaxKind.Variable:
            case SyntaxKind.ObjectValue:
                return false;

            case SyntaxKind.ListValue:
                var list = (ListValueNode)valueLiteral;
                for (var i = 0; i < list.Items.Count; i++)
                {
                    if (!CanBeCompiled(list.Items[i]))
                    {
                        return false;
                    }
                }
                break;
        }

        return true;
    }

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

        // at last we clear the rented lists
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

internal delegate FieldDelegate CreateFieldPipeline(ISchema schema, IObjectField field, FieldNode selection);
