using System.Buffers;
using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using HotChocolate.Properties;

namespace HotChocolate.Execution
{
    internal sealed class VariableToValueRewriter
        : SyntaxRewriter<object>
    {
        private static readonly ThreadLocal<VariableToValueRewriter> _current =
            new ThreadLocal<VariableToValueRewriter>(
                () => new VariableToValueRewriter());
        private readonly Stack<IType> _type = new Stack<IType>();
        private IVariableValueCollection _variables;
        private ITypeConversion _typeConversion;

        public IValueNode RewriteValue(
            IValueNode value,
            IType type,
            IVariableValueCollection variables,
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
                        rewritten = ReplaceVariable(value, field.Type);
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
                    .SetMessage(CoreResources.VarRewriter_UnknownField)
                    .SetCode(UtilityErrorCodes.UnknownField)
                    .Build());
        }

        protected override ListValueNode RewriteListValue(
            ListValueNode node,
            object context)
        {
            ListValueNode current = node;

            if (_type.Peek().ListType().ElementType is IInputType elementType)
            {
                _type.Push(_type.Peek().ListType().ElementType);

                if (current.Items.Count > 0)
                {
                    IValueNode[] rented = ArrayPool<IValueNode>.Shared.Rent(
                        current.Items.Count);
                    Span<IValueNode> copy = rented;
                    copy = copy.Slice(0, current.Items.Count);
                    bool rewrite = false;

                    for (int i = 0; i < current.Items.Count; i++)
                    {
                        IValueNode value = current.Items[i];

                        if (value is VariableNode variable)
                        {
                            rewrite = true;
                            copy[i] = ReplaceVariable(variable, elementType);
                        }
                        else
                        {
                            copy[i] = value;
                        }
                    }

                    if (rewrite)
                    {
                        var rewritten = new IValueNode[current.Items.Count];

                        for (int i = 0; i < current.Items.Count; i++)
                        {
                            rewritten[i] = copy[i];
                        }

                        current = current.WithItems(rewritten);
                    }

                    copy.Clear();
                    ArrayPool<IValueNode>.Shared.Return(rented);
                }

                _type.Pop();
            }

            return current;
        }

        private IValueNode ReplaceVariable(
            VariableNode variable,
            IInputType type)
        {
            if (_variables.TryGetVariable(
                variable.Name.Value,
                out object v))
            {
                if (!type.ClrType.IsInstanceOfType(v)
                    && !_typeConversion.TryConvert(
                        typeof(object),
                        type.ClrType,
                        v,
                        out v))
                {
                    throw new QueryException(
                        ErrorBuilder.New()
                            .SetMessage(CoreResources.VarRewriter_CannotConvert)
                            .SetCode(UtilityErrorCodes.NoConverter)
                            .AddLocation(variable)
                            .Build());
                }

                return type.ParseValue(v);
            }

            return type.ParseValue(null);
        }

        public static IValueNode Rewrite(
            IValueNode value,
            IType type,
            IVariableValueCollection variables,
            ITypeConversion typeConversion) =>
            _current.Value.RewriteValue(value, type, variables, typeConversion);
    }
}
