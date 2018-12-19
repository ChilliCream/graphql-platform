using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class ListType
        : TypeBase
        , IOutputType
        , IInputType
        , INullableType
    {
        private readonly bool _isInputType;
        private readonly IInputType _inputType;

        public ListType(IType elementType)
            : base(TypeKind.List)
        {
            if (elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            if (elementType.IsListType())
            {
                throw new ArgumentException(
                    "It is not possible to put a list type into list type.",
                    nameof(elementType));
            }

            _isInputType = elementType.IsInputType();
            _inputType = elementType as IInputType;

            ElementType = elementType;
            ClrType = this.ToClrType();
        }

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

        public IValueNode ParseValue(object value)
        {
            if (_isInputType)
            {
                if (value == null)
                {
                    return new NullValueNode(null);
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

            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }
    }

    // this is just a marker type for the fluent code-first api.
    public sealed class ListType<T>
        : IOutputType
        , IInputType
        where T : IType
    {
        private ListType()
        {
        }

        public Type ClrType => throw new NotImplementedException();

        public TypeKind Kind => throw new NotImplementedException();

        public bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public object ParseLiteral(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public IValueNode ParseValue(object value)
        {
            throw new NotImplementedException();
        }
    }
}
