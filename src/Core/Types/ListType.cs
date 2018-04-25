using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

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
                throw new ArgumentException(
                    "It is not possible to put a list type into list type.",
                    nameof(elementType));
            }

            _isInputType = elementType.InnerType().IsInputType();
            _inputType = elementType.InnerType() as IInputType;
            ElementType = elementType;
        }

        public IType ElementType { get; }

        public bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (_isInputType)
            {
                if (literal is NullValueNode)
                {
                    return true;
                }

                if (literal is ListValueNode listValueLiteral)
                {
                    if (listValueLiteral.Items.Any())
                    {
                        return true;
                    }
                    else
                    {
                        IValueNode value = listValueLiteral.Items.First();
                        if (!_inputType.IsInstanceOfType(value))
                        {
                            return !ElementType.IsNonNullType()
                                && value is NullValueNode;
                        }
                        return true;
                    }
                }
                return false;
            }

            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        public object ParseLiteral(IValueNode literal, Type targetType)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            if (_isInputType && literal is ListValueNode listValueLiteral)
            {
                if (targetType.IsArray)
                {
                    return CreateArray(literal, targetType, listValueLiteral);
                }

                if (targetType.IsGenericType)
                {
                    return CreateList(literal, targetType, listValueLiteral);
                }

                throw new NotSupportedException(
                    "The target type cannot be handled.");
            }

            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        private object CreateArray(IValueNode literal,
            Type targetType, ListValueNode listValueLiteral)
        {
            Type elementType = targetType.GetElementType();
            Array array = Array.CreateInstance(
                elementType,
                listValueLiteral.Items.Count);

            for (int i = 0; i < listValueLiteral.Items.Count; i++)
            {
                object element = _inputType.ParseLiteral(literal, elementType);
                array.SetValue(element, i);
            }

            return array;
        }

        private object CreateList(IValueNode literal,
           Type targetType, ListValueNode listValueLiteral)
        {
            Type elementType = targetType.GetGenericArguments()
                .SingleOrDefault();
            if (elementType != null && typeof(IList).IsAssignableFrom(targetType))
            {
                IList list = (IList)Activator.CreateInstance(targetType);

                for (int i = 0; i < listValueLiteral.Items.Count; i++)
                {
                    object element = _inputType.ParseLiteral(literal, elementType);
                    list.Add(element);
                }

                return list;
            }

            throw new NotSupportedException(
                "A list type must implement IList.");
        }
    }
}
