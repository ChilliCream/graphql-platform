using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Properties;
using ExtendedType = HotChocolate.Internal.ExtendedType;

#nullable enable

namespace HotChocolate.Types
{
    public class ListType
        : NonNamedType
        , INullableType
    {
        private readonly bool _isNestedList;
        private readonly IInputType? _namedType;

        public ListType(IType elementType)
            : base(elementType)
        {
            if (elementType.IsInputType())
            {
                _isNestedList = elementType.IsListType();
                _namedType = _isNestedList
                    ? (IInputType)elementType.ElementType()
                    : (IInputType)elementType;
            }
        }

        public override TypeKind Kind => TypeKind.List;

        public IType ElementType => InnerType;

        protected sealed override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                return true;
            }

            IInputType inputType = InnerInputType!;

            if (_namedType!.IsInstanceOfType(literal))
            {
                return true;
            }

            if (_isNestedList)
            {
                if (literal is ListValueNode listValueLiteral)
                {
                    foreach (IValueNode element in listValueLiteral.Items)
                    {
                        if (element.Kind != SyntaxKind.ListValue ||
                            !inputType.IsInstanceOfType(element))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            else
            {
                if (literal is ListValueNode listValueLiteral)
                {
                    foreach (IValueNode element in listValueLiteral.Items)
                    {
                        if (!inputType.IsInstanceOfType(element))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            return false;
        }

        protected sealed override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults)
        {
            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            if (valueSyntax.Kind != SyntaxKind.ListValue && 
                InnerInputType!.IsInstanceOfType(valueSyntax))
            {
                return CreateList(new ListValueNode(valueSyntax), withDefaults);
            }

            if (valueSyntax.Kind == SyntaxKind.ListValue)
            {
                if (_isNestedList)
                {
                    if (IsInstanceOfType(valueSyntax))
                    {
                        return CreateList((ListValueNode)valueSyntax, withDefaults);
                    }
                }
                else
                {
                    return CreateList((ListValueNode)valueSyntax, withDefaults);
                }
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(this.Print(), valueSyntax.GetType()),
                this);
        }

        protected sealed override bool IsInstanceOfType(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return true;
            }

            if (RuntimeType.IsInstanceOfType(runtimeValue))
            {
                return true;
            }

            Type? elementType = ExtendedType.Tools.GetElementType(runtimeValue.GetType());

            if (elementType is null)
            {
                return false;
            }

            Type clrType = InnerClrType;

            if (elementType == typeof(object))
            {
                return runtimeValue is IList l
                    && (l.Count == 0 || clrType == l[0]?.GetType());
            }

            return elementType == clrType;
        }

        protected sealed override IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return NullValueNode.Default;
            }

            if (runtimeValue is IList l)
            {
                var items = new List<IValueNode>();

                for (var i = 0; i < l.Count; i++)
                {
                    items.Add(InnerInputType!.ParseValue(l[i]));
                }

                return new ListValueNode(null, items);
            }
            
            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(this.Print(), runtimeValue.GetType()),
                this);
        }

        protected override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is IList l)
            {
                var items = new List<IValueNode>();

                for (var i = 0; i < l.Count; i++)
                {
                    items.Add(InnerInputType!.ParseResult(l[i]));
                }

                return new ListValueNode(items);
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseResult(this.Print(), resultValue.GetType()),
                this);
        }

        protected sealed override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is IList l)
            {
                var list = new List<object?>();

                for (var i = 0; i < l.Count; i++)
                {
                    list.Add(InnerInputType!.Serialize(l[i]));
                }

                resultValue = list;
                return true;
            }

            resultValue = null;
            return false;
        }

        protected sealed override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is IList l)
            {
                var list = (IList)Activator.CreateInstance(RuntimeType)!;

                for (var i = 0; i < l.Count; i++)
                {
                    if (InnerInputType!.TryDeserialize(l[i], out var v))
                    {
                        list.Add(v);
                    }
                    else
                    {
                        runtimeValue = null;
                        return false;
                    }
                }

                runtimeValue = list;
                return true;
            }

            runtimeValue = null;
            return false;
        }

        private object CreateList(ListValueNode listLiteral, bool withDefaults)
        {
            var list = (IList)Activator.CreateInstance(RuntimeType)!;

            for (var i = 0; i < listLiteral.Items.Count; i++)
            {
                list.Add(InnerInputType!.ParseLiteral(listLiteral.Items[i],withDefaults));
            }

            return list;
        }
    }
}
