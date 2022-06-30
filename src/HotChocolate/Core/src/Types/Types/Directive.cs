using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

public sealed class Directive : IDirective
{
    private object? _customDirective;
    private DirectiveNode? _parsedDirective;
    private Dictionary<string, ArgumentNode>? _arguments;

    private Directive(
        DirectiveType directiveType,
        DirectiveNode parsedDirective,
        object? customDirective,
        object source)
    {
        Type = directiveType
            ?? throw new ArgumentNullException(nameof(directiveType));
        _parsedDirective = parsedDirective
            ?? throw new ArgumentNullException(nameof(parsedDirective));
        _customDirective = customDirective;
        Source = source
            ?? throw new ArgumentNullException(nameof(source));
        Name = directiveType.Name;
    }

    public string Name { get; }

    public DirectiveType Type { get; }

    public object Source { get; }

    public IReadOnlyList<DirectiveMiddleware> MiddlewareComponents =>
        Type.MiddlewareComponents;

    public T ToObject<T>()
    {
        if (_customDirective is T d)
        {
            return d;
        }

        if (_customDirective is null)
        {
            d = CreateCustomDirective<T>();
            _customDirective = d;
            return d;
        }

        return CreateCustomDirective<T>();
    }

    public DirectiveNode ToNode() => ToNode(false);

    public DirectiveNode ToNode(bool removeNullArguments)
    {
        if (_parsedDirective is null)
        {
            _parsedDirective = ParseValue(Type, _customDirective!);
        }

        if (removeNullArguments
            && _parsedDirective.Arguments.Count != 0
            && _parsedDirective.Arguments.Any(t => t.Value.IsNull()))
        {
            var arguments = new List<ArgumentNode>();

            foreach (var argument in _parsedDirective.Arguments)
            {
                if (!argument.Value.IsNull())
                {
                    arguments.Add(argument);
                }
            }

            return _parsedDirective.WithArguments(arguments);
        }

        return _parsedDirective;
    }

    public T GetArgument<T>(string argumentName)
    {
        if (string.IsNullOrEmpty(argumentName))
        {
            throw new ArgumentNullException(nameof(argumentName));
        }

        var arguments = GetArguments();
        if (arguments.TryGetValue(argumentName, out var argValue)
            && Type.Arguments.TryGetField(argumentName, out var arg))
        {
            return Type.DeserializeArgument<T>(arg, argValue.Value);
        }

        throw new ArgumentException(
            TypeResources.Directive_GetArgument_ArgumentNameIsInvalid,
            nameof(argumentName));
    }


    private T CreateCustomDirective<T>()
    {
        if (TryDeserialize(_parsedDirective!, out T directive))
        {
            return directive;
        }

        directive = (T)Activator.CreateInstance(typeof(T))!;

        var properties =
            typeof(T).GetProperties()
                .ToLookup(t => t.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var argument in Type.Arguments)
        {
            var property = properties[argument.Name].FirstOrDefault();

            if (property != null)
            {
                SetProperty(argument, directive, property);
            }
        }

        return directive;
    }

    private void SetProperty(
        Argument argument,
        object obj,
        PropertyInfo property)
    {
        var arguments = GetArguments();
        if (arguments.TryGetValue(argument.Name, out var argumentValue))
        {
            var parsedValue = Type.DeserializeArgument(
                argument, argumentValue.Value, property.PropertyType);

            property.SetValue(obj, parsedValue);
        }
    }

    private Dictionary<string, ArgumentNode> GetArguments()
        => _arguments ??= ToNode().Arguments.ToDictionary(t => t.Name.Value);

    private bool TryDeserialize<T>(
        DirectiveNode directiveNode,
        out T directive)
    {
        var constructor = typeof(T).GetTypeInfo()
            .DeclaredConstructors.FirstOrDefault(t =>
            {
                var parameters = t.GetParameters();
                return parameters.Length == 2
                    && parameters[0].ParameterType ==
                        typeof(SerializationInfo)
                    && parameters[1].ParameterType ==
                        typeof(StreamingContext);
            });

        if (constructor is null)
        {
            directive = default!;
            return false;
        }

        var info = new SerializationInfo(
            typeof(T), new FormatterConverter());
        info.AddValue(nameof(DirectiveNode), directiveNode);

        var context = new StreamingContext(
            StreamingContextStates.Other,
            this);

        directive = (T)constructor.Invoke(new object[] { info, context });
        return true;
    }

    public static Directive FromDescription(
        DirectiveType directiveType,
        DirectiveDefinition definition,
        object source)
    {
        if (directiveType is null)
        {
            throw new ArgumentNullException(nameof(directiveType));
        }

        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (definition.CustomDirective is null)
        {
            return new Directive(
                directiveType,
                CompleteArguments(directiveType, definition.ParsedDirective!),
                null,
                source);
        }

        var directiveNode = ParseValue(
            directiveType, definition.CustomDirective);

        return new Directive(
            directiveType,
            CompleteArguments(directiveType, directiveNode),
            definition.CustomDirective,
            source);
    }

    public static Directive FromAstNode(
        ISchema schema,
        ISyntaxNode source,
        DirectiveNode directiveNode)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (directiveNode is null)
        {
            throw new ArgumentNullException(nameof(directiveNode));
        }

        if (schema.TryGetDirectiveType(
            directiveNode.Name.Value,
            out var type))
        {
            return new Directive(
                type,
                CompleteArguments(type, directiveNode),
                null,
                source);
        }

        throw new InvalidOperationException(
            "The specified directive is not registered " +
            "with the given schema.");
    }

    private static DirectiveNode CompleteArguments(
        DirectiveType directiveType,
        DirectiveNode directive)
    {
        if (directiveType.Arguments.Count > 0
            && directiveType.Arguments.Any(t => t.DefaultValue is { }))
        {
            List<ArgumentNode>? arguments = null;

            var argumentNames = new HashSet<string>(
                directive.Arguments.Select(t => t.Name.Value));

            foreach (var argument in directiveType.Arguments)
            {
                if (argument.DefaultValue is { }
                    && !argumentNames.Contains(argument.Name))
                {
                    arguments ??= new List<ArgumentNode>();
                    arguments.Add(new ArgumentNode(argument.Name, argument.DefaultValue));
                }
            }

            if (arguments is { })
            {
                arguments.AddRange(directive.Arguments);
                return directive.WithArguments(arguments);
            }
        }

        return directive;
    }

    private static DirectiveNode ParseValue(
        DirectiveType directiveType,
        object directive)
    {
        var arguments = new List<ArgumentNode>();

        var type = directive.GetType();
        var properties =
            type.GetProperties().ToLookup(t => t.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var argument in directiveType.Arguments)
        {
            var property = properties[argument.Name].FirstOrDefault();
            var propertyValue = property?.GetValue(directive);

            var valueNode = directiveType.SerializeArgument(argument, propertyValue);
            arguments.Add(new ArgumentNode(argument.Name, valueNode));
        }

        return new DirectiveNode(directiveType.Name, arguments);
    }
}
