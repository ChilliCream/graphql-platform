using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;
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

        bool IInputType.IsInstanceOfType(IValueNode literal)
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

        object IInputType.ParseLiteral(IValueNode literal)
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

            if (literal is ListValueNode listValueLiteral)
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

        bool IInputType.IsInstanceOfType(object value)
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

                if (elementType is null)
                {
                    return false;
                }

                if (elementType == typeof(object))
                {
                    if(value is IList l)
                    {
                        return l.Count == 0 || elementType == l[0]?.GetType();
                    }

                    if(value is IEnumerable<object> e)
                    {
                        if(e.Any())
                        {
                            return  elementType == e.First()?.GetType();
                        }
                        else
                        {
                            return true;
                        }
                    }

                    return false;
                }

                return elementType == ElementType.ToClrType();
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        IValueNode IInputType.ParseValue(object value)
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

        object ISerializableType.Serialize(object value)
        {
            if (_isInputType)
            {
                if (value is null)
                {
                    return null;
                }

                if (value is IList l)
                {
                    var list = new List<object>();

                    for (int i = 0; i < l.Count; i++)
                    {
                        list.Add(_inputType.Serialize(l[i]));
                    }

                    return list;
                }

                if (value.GetType() != typeof(string) && value is IEnumerable e)
                {
                    var list = new List<object>();

                    foreach (object obj in e)
                    {
                        list.Add(_inputType.Serialize(obj));
                    }

                    return list;
                }

                // TODO : resources
                throw new ScalarSerializationException(
                    TypeResourceHelper.Scalar_Cannot_Serialize(
                        this.Visualize()));
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        object ISerializableType.Deserialize(object serialized)
        {
            if (_isInputType)
            {
                if (TryDeserialize(serialized, out var value))
                {
                    return value;
                }

                throw new ScalarSerializationException(
                    TypeResourceHelper.Scalar_Cannot_Deserialize(
                        this.Visualize()));
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        bool ISerializableType.TryDeserialize(
            object serialized, out object value)
        {
            if (_isInputType)
            {
                return TryDeserialize(serialized, out value);
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        private bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is IList l)
            {
                var list = (IList)Activator.CreateInstance(ClrType);

                for (int i = 0; i < l.Count; i++)
                {
                    if (_inputType.TryDeserialize(l[i], out var v))
                    {
                        list.Add(v);
                    }
                    else
                    {
                        value = null;
                        return false;
                    }
                }

                value = list;
                return true;
            }

            if (serialized.GetType() != typeof(string)
                && serialized is IEnumerable e)
            {
                var list = (IList)Activator.CreateInstance(ClrType);

                foreach (object obj in e)
                {
                    if (_inputType.TryDeserialize(obj, out var v))
                    {
                        list.Add(v);
                    }
                    else
                    {
                        value = null;
                        return false;
                    }
                }

                value = list;
                return true;
            }

            value = null;
            return false;
        }
    }
}
