using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
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
                        rewritten = ReplaceVariable(value, field);
                        break;

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

            // TODO : resource
            throw new QueryException(
                ErrorBuilder.New()
                    .SetMessage("Unknown field.")
                    .Build());
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

        private IValueNode ReplaceVariable(
            VariableNode variable,
            IInputField field)
        {
            if (_variables.TryGetVariable(
                variable.Name.Value,
                out object v))
            {
                if (!field.Type.ClrType.IsInstanceOfType(v)
                    && !_typeConversion.TryConvert(
                        typeof(object),
                        field.Type.ClrType,
                        v,
                        out v))
                {
                    // TODO : resource
                    throw new QueryException(
                        ErrorBuilder.New()
                            .SetMessage(
                                "Unable to convert the specified " +
                                "variable value.")
                            .Build());
                }

                return field.Type.ParseValue(v);
            }

            return field.Type.ParseValue(null);
        }

        public static IValueNode Rewrite(
            IValueNode value,
            IType type,
            IVariableCollection variables,
            ITypeConversion typeConversion) =>
            _current.Value.RewriteValue(value, type, variables, typeConversion);
    }
}
