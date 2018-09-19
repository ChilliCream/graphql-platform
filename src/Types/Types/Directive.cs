using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal sealed class Directive
        : IDirective
    {
        private object _customDirective;
        private DirectiveNode _parsedDirective;
        private Dictionary<string, ArgumentNode> _arguments;

        public Directive(
            DirectiveType directiveType,
            DirectiveNode parsedDirective)
        {
            Type = directiveType
                ?? throw new ArgumentNullException(nameof(directiveType));
            _parsedDirective = parsedDirective
                ?? throw new ArgumentNullException(nameof(parsedDirective));
            Name = directiveType.Name;
        }

        public Directive(DirectiveType directiveType, object customDirective)
        {
            Type = directiveType
                ?? throw new ArgumentNullException(nameof(directiveType));
            _customDirective = customDirective
                ?? throw new ArgumentNullException(nameof(customDirective));
            Name = directiveType.Name;
        }

        public string Name { get; }

        public DirectiveType Type { get; }

        public OnBeforeInvokeResolver OnBeforeInvokeResolver =>
            Type.OnBeforeInvokeResolver;

        public DirectiveResolver OnInvokeResolver =>
            Type.OnInvokeResolver;

        public OnAfterInvokeResolver OnAfterInvokeResolver =>
            Type.OnAfterInvokeResolver;

        public bool IsExecutable => Type.IsExecutable;

        public T ToObject<T>()
        {
            if (_customDirective is T d)
            {
                return d;
            }

            return default;
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
                if (arg.Type is InputObjectType iot
                    && !typeof(T).IsAssignableFrom(iot.ClrType))
                {
                    return (T)InputObjectDefaultDeserializer.ParseLiteral(
                        iot, typeof(T), (ObjectValueNode)argValue.Value);
                }

                return (T)arg.Type.ParseLiteral(argValue.Value);
            }

            throw new ArgumentException(
                "The argument name is invalid.",
                nameof(argumentName));
        }


        private T DeserializeNode<T>()
        {
            DirectiveNode node = ToNode();
            Type clrType = typeof(T);
            object obj = Activator.CreateInstance(typeof(T));

            foreach (ArgumentNode argumentValue in node.Arguments)
            {
                if (Type.Arguments.TryGetField(
                    argumentValue.Name.Value,
                    out InputField argument))
                {
                    DeserializeArgument(argument, argumentValue, clrType, obj);
                }
            }

            return (T)obj;
        }

        private void DeserializeArgument(
            InputField argument,
            ArgumentNode argumentValue,
            Type clrType,
            object obj)
        {
            PropertyInfo property;
            if (argument.Property == null)
            {
                property = clrType.GetProperties().FirstOrDefault(
                    t => t.Name.Equals(argument.Name,
                        StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                property = argument.Property;
            }

            object value = argument.Type.ParseLiteral(argumentValue.Value);
            property.SetValue(obj, value);
        }

        private Dictionary<string, ArgumentNode> GetArguments()
        {
            if (_arguments == null)
            {
                _arguments = ToNode().Arguments.ToDictionary(t => t.Name.Value);
            }

            return _arguments;
        }

        private static DirectiveNode SerializeCustomDirective(DirectiveType directiveType, object customDirective)
        {
            throw new NotImplementedException();
        }

        internal static Directive FromDescription(
            DirectiveType directiveType,
            DirectiveDescription description)
        {
            if (directiveType == null)
            {
                throw new ArgumentNullException(nameof(directiveType));
            }

            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (description.CustomDirective is null)
            {
                return new Directive(directiveType,
                    description.ParsedDirective);
            }
            else
            {
                return new Directive(directiveType,
                    description.CustomDirective);
            }
        }
    }
}
