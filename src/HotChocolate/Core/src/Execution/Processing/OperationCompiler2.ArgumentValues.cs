using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler2
{
    private IArgumentMap? CoerceArgumentValues(
        IObjectField field,
        FieldNode selection,
        string responseName)
    {
        if (field.Arguments.Count == 0)
        {
            return null;
        }

        var arguments = new Dictionary<NameString, ArgumentValue>();

        for (var i = 0; i < selection.Arguments.Count; i++)
        {
            ArgumentNode argumentValue = selection.Arguments[i];
            if (field.Arguments.TryGetField(
                argumentValue.Name.Value,
                out IInputField? argument))
            {
                arguments[argument.Name.Value] =
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
            IInputField argument = field.Arguments[i];
            if (!arguments.ContainsKey(argument.Name))
            {
                arguments[argument.Name.Value] =
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
        ArgumentNonNullValidator.ValidationResult validationResult = ArgumentNonNullValidator
            .Validate(argument, value, PathFactory.Instance.New(argument.Name));

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

    private static FieldDelegate CreateFieldMiddleware(
        ISchema schema,
        IObjectField field,
        FieldNode selection)
    {
        FieldDelegate pipeline = field.Middleware;

        if (field.ExecutableDirectives.Count == 0 && selection.Directives.Count == 0)
        {
            return pipeline;
        }

        IReadOnlyList<IDirective> directives = CollectDirectives(schema, field, selection);

        if (directives.Count > 0)
        {
            pipeline = Compile(pipeline, directives);
        }

        return pipeline;
    }

    private static PureFieldDelegate? TryCreatePureField(
        IObjectField field,
        FieldNode selection)
    {
        if (field.PureResolver is not null && selection.Directives.Count == 0)
        {
            return field.PureResolver;
        }

        return null;
    }

    private static IReadOnlyList<IDirective> CollectDirectives(
        ISchema schema,
        IObjectField field,
        FieldNode selection)
    {
        var processed = new HashSet<string>();
        var directives = new List<IDirective>();

        CollectTypeSystemDirectives(
            processed,
            directives,
            field);

        CollectQueryDirectives(
            schema,
            processed,
            directives,
            field,
            selection);

        return directives.AsReadOnly();
    }

    private static void CollectQueryDirectives(
        ISchema schema,
        HashSet<string> processed,
        List<IDirective> directives,
        IObjectField field,
        FieldNode selection)
    {
        foreach (IDirective directive in GetFieldSelectionDirectives(schema, field, selection))
        {
            if (!directive.Type.IsRepeatable && !processed.Add(directive.Name))
            {
                directives.Remove(directives.First(t => t.Type == directive.Type));
            }
            directives.Add(directive);
        }
    }

    private static IEnumerable<IDirective> GetFieldSelectionDirectives(
        ISchema schema,
        IObjectField field,
        FieldNode selection)
    {
        for (var i = 0; i < selection.Directives.Count; i++)
        {
            DirectiveNode directive = selection.Directives[i];
            if (schema.TryGetDirectiveType(directive.Name.Value,
                out DirectiveType? directiveType)
                && directiveType.HasMiddleware)
            {
                yield return Directive.FromDescription(
                    directiveType,
                    new DirectiveDefinition(directive),
                    field);
            }
        }
    }

    private static void CollectTypeSystemDirectives(
        HashSet<string> processed,
        List<IDirective> directives,
        IObjectField field)
    {
        for (var i = 0; i < field.ExecutableDirectives.Count; i++)
        {
            IDirective directive = field.ExecutableDirectives[i];
            if (!directive.Type.IsRepeatable && !processed.Add(directive.Name))
            {
                directives.Remove(directives.First(t => t.Type == directive.Type));
            }
            directives.Add(directive);
        }
    }

    private static FieldDelegate Compile(
        FieldDelegate fieldPipeline,
        IReadOnlyList<IDirective> directives)
    {
        FieldDelegate next = fieldPipeline;

        for (var i = directives.Count - 1; i >= 0; i--)
        {
            if (directives[i] is { Type: { HasMiddleware: true } } directive)
            {
                next = BuildComponent(directive, next);
            }
        }

        return next;
    }

    private static FieldDelegate BuildComponent(IDirective directive, FieldDelegate first)
    {
        FieldDelegate next = first;
        IReadOnlyList<DirectiveMiddleware> components = directive.MiddlewareComponents;

        for (var i = components.Count - 1; i >= 0; i--)
        {
            DirectiveDelegate component = components[i].Invoke(next);
            next = context => HasNoErrors(context.Result)
                ? component.Invoke(new DirectiveContext(context, directive))
                : default;
        }

        return next;
    }

    private static bool HasNoErrors(object? result)
        => result is not IError or not IEnumerable<IError>;
}
