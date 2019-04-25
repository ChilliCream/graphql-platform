using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public class ListType
        : IOutputType
        , IInputType
        , INullableType
    {
        private readonly bool _isInputType;
        private readonly IInputType _inputType;

        public ListType(IType elementType)
        {
            if (elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            if (elementType.IsListType())
            {
                // TODO : resources
                throw new ArgumentException(
                    "It is not possible to put a list type into list type.",
                    nameof(elementType));
            }

            _isInputType = elementType.IsInputType();
            _inputType = elementType as IInputType;

            ElementType = elementType;
            ClrType = this.ToClrType();
        }

        public TypeKind Kind => TypeKind.List;

        public IType ElementType { get; }

        public Type ClrType { get; }

        public bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (_isInputType)
            {
                return IsInstanceOfTypeInternal(literal);
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        private bool IsInstanceOfTypeInternal(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                return true;
            }

            if (_inputType.IsInstanceOfType(literal))
            {
                return true;
            }

            if (literal is ListValueNode listValueLiteral)
            {
                foreach (IValueNode element in listValueLiteral.Items)
                {
                    if (!_inputType.IsInstanceOfType(element))
                    {
                        return false;
                    }
                }

                return true;
            }
            return false;
        }

        public object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (_isInputType)
            {
                return ParseLiteralInternal(literal);
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        private object ParseLiteralInternal(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                return null;
            }

            if (_inputType.IsInstanceOfType(literal))
            {
                return CreateArray(new ListValueNode(literal));
            }

            if (_isInputType && literal is ListValueNode listValueLiteral)
            {
                return CreateArray(listValueLiteral);
            }

            // TODO : resources
            throw new ArgumentException(
                "The specified literal cannot be handled by this list type.");
        }

        private object CreateArray(ListValueNode listValueLiteral)
        {
            Type elementType = _inputType.ClrType;
            var array = Array.CreateInstance(
                elementType,
                listValueLiteral.Items.Count);

            for (var i = 0; i < listValueLiteral.Items.Count; i++)
            {
                object element = _inputType.ParseLiteral(listValueLiteral.Items[i]);
                array.SetValue(element, i);
            }

            return array;
        }

        public bool IsInstanceOfType(object value)
        {
            if (_isInputType)
            {
                if (value is null)
                {
                    return true;
                }

                if (ClrType.IsInstanceOfType(value))
                {
                    return true;
                }

                Type elementType = DotNetTypeInfoFactory
                    .GetInnerListType(value.GetType());

                if (elementType == null)
                {
                    return false;
                }

                return elementType == ElementType.ToClrType();
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        public IValueNode ParseValue(object value)
        {
            if (_isInputType)
            {
                if (value == null)
                {
                    return NullValueNode.Default;
                }

                if (value is IEnumerable e)
                {
                    var items = new List<IValueNode>();
                    foreach (object v in e)
                    {
                        items.Add(_inputType.ParseValue(v));
                    }
                    return new ListValueNode(null, items);
                }
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }
    }
}
