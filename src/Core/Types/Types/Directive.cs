using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    internal sealed class Directive
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

        public string Name { get; }

        public DirectiveType Type { get; }

        public object Source { get; }

        public DirectiveMiddleware Middleware  => Type.Middleware;

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

        public DirectiveNode ToNode()
        {
            if (_parsedDirective is null)
            {
                var arguments = new List<ArgumentNode>();
                Type type = _customDirective.GetType();
                ILookup<string, PropertyInfo> properties = type.GetProperties()
                    .ToLookup(t => t.Name, StringComparer.OrdinalIgnoreCase);

                foreach (InputField argument in Type.Arguments)
                {
                    PropertyInfo property =
                        properties[argument.Name].FirstOrDefault();
                    var value = property?.GetValue(_customDirective);

                    IValueNode valueNode = argument.Type.ParseValue(value);
                    arguments.Add(new ArgumentNode(argument.Name, valueNode));
                }

                _parsedDirective = new DirectiveNode(Name, arguments);
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
                && Type.Arguments.TryGetField(argumentName, out InputField arg))
            {
                if (typeof(T).IsAssignableFrom(arg.Type.ClrType))
                {
                    return (T)arg.Type.ParseLiteral(argValue.Value);
                }
                else
                {
                    return ValueDeserializer
                        .ParseLiteral<T>(arg.Type, argValue.Value);
                }
            }

            throw new ArgumentException(
                "The argument name is invalid.",
                nameof(argumentName));
        }


        private T CreateCustomDirective<T>()
        {
            object obj = Activator.CreateInstance(typeof(T));

            ILookup<string, PropertyInfo> properties =
                typeof(T).GetProperties()
                    .ToLookup(t => t.Name, StringComparer.OrdinalIgnoreCase);

            foreach (InputField argument in Type.Arguments)
            {
                PropertyInfo property = properties[argument.Name]
                    .FirstOrDefault();

                if (property != null)
                {
                    SetProperty(argument, obj, property);
                }
            }

            return (T)obj;
        }

        private void SetProperty(InputField argument, object obj, PropertyInfo property)
        {
            Dictionary<string, ArgumentNode> arguments = GetArguments();
            if (arguments.TryGetValue(argument.Name,
                out ArgumentNode argumentValue))
            {
                object parsedValue = ValueDeserializer.ParseLiteral(
                    argument.Type, property.PropertyType, argumentValue.Value);

                ValueDeserializer.SetProperty(
                    property, argument.Type.IsListType(),
                    obj, parsedValue);
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

        internal static Directive FromDescription(
            DirectiveType directiveType,
            DirectiveDescription description,
            object source)
        {
            if (directiveType == null)
            {
                throw new ArgumentNullException(nameof(directiveType));
            }

            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (description.CustomDirective is null)
            {
                return new Directive(directiveType,
                    description.ParsedDirective,
                    source);
            }
            else
            {
                return new Directive(directiveType,
                    description.CustomDirective,
                    source);
            }
        }
    }
}
