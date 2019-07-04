using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public sealed class Directive
        : IDirective
    {
        private object _customDirective;
        private DirectiveNode _parsedDirective;
        private Dictionary<string, ArgumentNode> _arguments;

        internal Directive(
            DirectiveType directiveType,
            DirectiveNode parsedDirective,
            object source)
        {
            Type = directiveType
                ?? throw new ArgumentNullException(nameof(directiveType));
            _parsedDirective = parsedDirective
                ?? throw new ArgumentNullException(nameof(parsedDirective));
            Source = source
                ?? throw new ArgumentNullException(nameof(source));
            Name = directiveType.Name;
        }

        internal Directive(
            DirectiveType directiveType,
            object customDirective,
            object source)
        {
            Type = directiveType
                ?? throw new ArgumentNullException(nameof(directiveType));
            _customDirective = customDirective
                ?? throw new ArgumentNullException(nameof(customDirective));
            Source = source
                ?? throw new ArgumentNullException(nameof(source));
            Name = directiveType.Name;
        }

        public NameString Name { get; }

        public DirectiveType Type { get; }

        public object Source { get; }

        public IReadOnlyList<DirectiveMiddleware> MiddlewareComponents =>
            Type.MiddlewareComponents;

        public bool IsExecutable => Type.IsExecutable;

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
                var arguments = new List<ArgumentNode>();
                Type type = _customDirective.GetType();
                ILookup<string, PropertyInfo> properties = type.GetProperties()
                    .ToLookup(t => t.Name, StringComparer.OrdinalIgnoreCase);

                foreach (Argument argument in Type.Arguments)
                {
                    PropertyInfo property =
                        properties[argument.Name].FirstOrDefault();
                    var value = property?.GetValue(_customDirective);

                    IValueNode valueNode = argument.Type.ParseValue(value);
                    arguments.Add(new ArgumentNode(argument.Name, valueNode));
                }

                _parsedDirective = new DirectiveNode(Name, arguments);
            }

            if (removeNullArguments
                && _parsedDirective.Arguments.Count != 0
                && _parsedDirective.Arguments.Any(t => t.Value.IsNull()))
            {
                var arguments = new List<ArgumentNode>();
                foreach (ArgumentNode argument in _parsedDirective.Arguments)
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

            Dictionary<string, ArgumentNode> arguments = GetArguments();
            if (arguments.TryGetValue(argumentName, out ArgumentNode argValue)
                && Type.Arguments.TryGetField(argumentName, out Argument arg))
            {
                if (typeof(T).IsAssignableFrom(arg.Type.ClrType))
                {
                    return (T)arg.Type.ParseLiteral(argValue.Value);
                }
                else
                {
                    return Type.DeserializeArgument<T>(arg, argValue.Value);
                }
            }

            throw new ArgumentException(
                "The argument name is invalid.",
                nameof(argumentName));
        }


        private T CreateCustomDirective<T>()
        {
            if (TryDeserialize(_parsedDirective, out T directive))
            {
                return directive;
            }

            directive = (T)Activator.CreateInstance(typeof(T));

            ILookup<string, PropertyInfo> properties =
                typeof(T).GetProperties()
                    .ToLookup(t => t.Name, StringComparer.OrdinalIgnoreCase);

            foreach (Argument argument in Type.Arguments)
            {
                PropertyInfo property = properties[argument.Name]
                    .FirstOrDefault();

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
            Dictionary<string, ArgumentNode> arguments = GetArguments();
            if (arguments.TryGetValue(argument.Name,
                out ArgumentNode argumentValue))
            {
                object parsedValue = Type.DeserializeArgument(
                    argument, argumentValue.Value, property.PropertyType);

                property.SetValue(obj, parsedValue);
            }
        }

        private Dictionary<string, ArgumentNode> GetArguments()
        {
            if (_arguments == null)
            {
                _arguments = ToNode().Arguments.ToDictionary(t => t.Name.Value);
            }
            return _arguments;
        }

        private bool TryDeserialize<T>(
            DirectiveNode directiveNode,
            out T directive)
        {

            ConstructorInfo constructor = typeof(T).GetTypeInfo()
                .DeclaredConstructors.FirstOrDefault(t =>
                {
                    ParameterInfo[] parameters = t.GetParameters();
                    return parameters.Length == 2
                        && parameters[0].ParameterType ==
                            typeof(SerializationInfo)
                        && parameters[1].ParameterType ==
                            typeof(StreamingContext);
                });

            if (constructor == null)
            {
                directive = default;
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

        internal static Directive FromDescription(
            DirectiveType directiveType,
            DirectiveDefinition definition,
            object source)
        {
            if (directiveType == null)
            {
                throw new ArgumentNullException(nameof(directiveType));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (definition.CustomDirective is null)
            {
                return new Directive(directiveType,
                    definition.ParsedDirective,
                    source);
            }
            else
            {
                return new Directive(directiveType,
                    definition.CustomDirective,
                    source);
            }
        }

        public static Directive FromAstNode(
            ISchema schema,
            ISyntaxNode source,
            DirectiveNode directiveNode)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (directiveNode == null)
            {
                throw new ArgumentNullException(nameof(directiveNode));
            }

            if (schema.TryGetDirectiveType(
                directiveNode.Name.Value,
                out DirectiveType type))
            {
                return new Directive(type, directiveNode, source);
            }

            throw new InvalidOperationException(
                "The specified directive is not registered " +
                "with the given schema.");
        }
    }
}
