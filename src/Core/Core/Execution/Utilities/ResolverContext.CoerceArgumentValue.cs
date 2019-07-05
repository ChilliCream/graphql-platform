using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal partial class ResolverContext
        : IMiddlewareContext
    {
        public T Argument<T>(NameString name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_arguments.TryGetValue(name, out ArgumentValue argumentValue))
            {
                return CoerceArgumentValue<T>(name, argumentValue);
            }

            return default;
        }

        // TODO : simplify
        private T CoerceArgumentValue<T>(
            string name,
            ArgumentValue argumentValue)
        {
            object value = argumentValue.Value;

            if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
            {
                if (argumentValue.Literal == null)
                {
                    return (T)argumentValue.Type.ParseValue(value);
                }
                else
                {
                    return (T)argumentValue.Literal;
                }
            }

            if (argumentValue.Literal != null)
            {
                value = argumentValue.Type.ParseLiteral(argumentValue.Literal);
            }

            if (value is null)
            {
                return default;
            }

            if (value is T resolved)
            {
                return resolved;
            }

            if (TryConvertValue(
                argumentValue.Type.ClrType,
                value, out resolved))
            {
                return resolved;
            }

            IError error = ErrorBuilder.New()
                .SetMessage(string.Format(
                    CultureInfo.InvariantCulture,
                    CoreResources.ResolverContext_ArgumentConversion,
                    name,
                    argumentValue.Type.ClrType.FullName,
                    typeof(T).FullName))
                .SetPath(Path)
                .AddLocation(FieldSelection)
                .Build();

            throw new QueryException(error);
        }

        private bool TryConvertValue<T>(
            Type type,
            object value,
            out T converted)
        {
            if (Converter.TryConvert(
                type, typeof(T),
                value, out object c))
            {
                converted = (T)c;
                return true;
            }

            converted = default;
            return false;
        }
    }

    public sealed class VariableToValueRewriter
        : SyntaxRewriter<object>
    {
        private static readonly ThreadLocal<VariableToValueRewriter> _current =
            new ThreadLocal<VariableToValueRewriter>(
                () => new VariableToValueRewriter());
        private readonly Stack<IType> _type = new Stack<IType>();
        public IVariableCollection _variables;
        public ITypeConversion _typeConversion;

        public IValueNode RewriteValue(
            IValueNode value,
            IType type,
            IVariableCollection variables,
            ITypeConversion typeConversion)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (variables is null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            if (typeConversion is null)
            {
                throw new ArgumentNullException(nameof(typeConversion));
            }

            _variables = variables;
            _typeConversion = typeConversion;

            _type.Clear();
            _type.Push(type);

            return RewriteValue(value, null);
        }

        protected override ObjectFieldNode RewriteObjectField(
            ObjectFieldNode node,
            object context)
        {
            if (_type.Peek().NamedType() is InputObjectType inputObject
                && inputObject.Fields.TryGetField(
                    node.Name.Value,
                    out InputField field))
            {
                IValueNode rewritten = null;
                _type.Push(field.Type);

                switch (node.Value)
                {
                    case ListValueNode value:
                        rewritten = RewriteListValue(value, context);
                        break;

                    case ObjectValueNode value:
                        rewritten = RewriteObjectValue(value, context);
                        break;

                    case VariableNode value:


                    default:
                        rewritten = node.Value;
                        break;
                }

                _type.Pop();

                if (rewritten == node.Value)
                {
                    return node;
                }
                return node.WithValue(rewritten);
            }

            // TODO : Resources
            throw new InvalidOperationException("Unknown field type.");
        }

        protected override ListValueNode RewriteListValue(
            ListValueNode node,
            object context)
        {
            _type.Push(_type.Peek().ListType().ElementType);

            ListValueNode current = node;

            current = RewriteMany(current, current.Items, context,
                RewriteValue, current.WithItems);

            _type.Pop();

            return current;
        }

        private IValueNode ReplaceVariable(VariableValue variableValue)
        {
            if (_variables.TryGetVariable(
                value.Name.Value,
                out object v))
            {
                if (!field.Type.ClrType.IsInstanceOfType(v))
                {

                }
            }
        }

        public static IValueNode Rewrite(
            IValueNode value,
            IType type,
            IVariableCollection variables) =>
            _current.Value.RewriteValue(value, type, variables);
    }
}
