using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Utilities;

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

        protected override ObjectValueNode RewriteObjectValue(
            ObjectValueNode node,
            object context)
        {
            return base.RewriteObjectValue(node, node);
        }

        protected override ObjectFieldNode RewriteObjectField(
            ObjectFieldNode node,
            object context)
        {
            INamedType namedType = _type.Peek().NamedType();
            IInputType fieldType = null;

            if (namedType is ScalarType scalar
                && scalar.IsInstanceOfType((ObjectValueNode)context))
            {
                fieldType = scalar;
            }
            else if (namedType is InputObjectType inputObject
                && inputObject.Fields.TryGetField(
                    node.Name.Value,
                    out InputField field))
            {
                fieldType = field.Type;
            }

            if (fieldType != null)
            {
                IValueNode rewritten = null;
                _type.Push(fieldType);

                switch (node.Value)
                {
                    case ListValueNode value:
                        rewritten = RewriteListValue(value, context);
                        break;

                    case ObjectValueNode value:
                        rewritten = RewriteObjectValue(value, context);
                        break;

                    case VariableNode value:
                        rewritten = ReplaceVariable(value, fieldType);
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
            IType type = _type.Peek();
            IInputType elementType = null;

            if (type.IsListType() && type.ListType().ElementType is IInputType inputType)
            {
                elementType = inputType;
            }
            else if (type.NamedType() is ScalarType scalar && scalar.IsInstanceOfType(node))
            {
                elementType = scalar;
            }

            ListValueNode current = node;

            if (elementType != null)
            {
                _type.Push(elementType);

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
                            IValueNode rewritten = RewriteValue(value, context);
                            rewrite = rewritten != value;
                            copy[i] = rewritten;
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
                if (v is IValueNode n)
                {
                    return n;
                }

                if (v is { }
                    && type.IsListType()
                    && !(v is IList))
                {
                    Array array = Array.CreateInstance(v.GetType(), 1);
                    array.SetValue(v, 0);
                    v = array;
                }

                if (v is { }
                    && !type.ClrType.IsInstanceOfType(v)
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
