using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal class InputObjectToObjectValueConverter
    {
        private readonly ITypeConversion _converter;

        public InputObjectToObjectValueConverter(ITypeConversion converter)
        {
            _converter = converter
                ?? throw new ArgumentNullException(nameof(converter));
        }

        public ObjectValueNode Convert(
            InputObjectType type,
            object obj)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            ObjectValueNode objectValueNode = null;
            Action<IValueNode> setValue = value => objectValueNode = (ObjectValueNode)value;
            VisitInputObject(type, obj, setValue, new HashSet<object>());
            return objectValueNode;
        }

        private void VisitValue(
            IInputType type, object obj,
            Action<IValueNode> setValue,
            ISet<object> processed)
        {
            if (obj is null)
            {
                setValue(NullValueNode.Default);
            }
            else if (type.IsListType())
            {
                VisitList(
                    (ListType)type.ListType(),
                    obj, setValue, processed);
            }
            else if (type.IsLeafType())
            {
                VisitLeaf(
                    (INamedInputType)type.NamedType(),
                    obj, setValue, processed);
            }
            else if (type.IsInputObjectType())
            {
                VisitInputObject(
                    (InputObjectType)type.NamedType(),
                    obj, setValue, processed);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private void VisitInputObject(
            InputObjectType type, object obj,
            Action<IValueNode> setValue,
            ISet<object> processed)
        {
            if (obj is IOptional outerOptional)
            {
                if (outerOptional.HasValue && outerOptional.Value is null)
                {
                    setValue(NullValueNode.Default);
                    return;
                }

                obj = outerOptional.Value;
            }

            if (processed.Add(obj))
            {
                var fields = new List<ObjectFieldNode>();

                foreach (InputField field in type.Fields)
                {
                    if (field.TryGetValue(obj, out object fieldValue))
                    {
                        if (fieldValue is IOptional optional && !optional.HasValue)
                        {
                            continue;
                        }

                        Action<IValueNode> setField = value =>
                        {
                            value = field.Serializer is null 
                                ? value 
                                : field.Serializer.Rewrite(value);
                            fields.Add(new ObjectFieldNode(field.Name, value));
                        };
                        VisitValue(field.Type, fieldValue, setField, processed);
                    }
                }

                setValue(new ObjectValueNode(fields));
            }
        }

        private void VisitList(
            ListType type, object obj,
            Action<IValueNode> setValue,
            ISet<object> processed)
        {
            if (obj is IOptional optional && optional.HasValue)
            {
                obj = optional.Value;
            }

            if (obj is IEnumerable sourceList)
            {
                var list = new List<IValueNode>();
                var itemType = (IInputType)type.ElementType;
                Action<IValueNode> addItem = item => list.Add(item);

                foreach (object item in sourceList)
                {
                    VisitValue(itemType, item, addItem, processed);
                }

                setValue(new ListValueNode(list));
            }
        }

        private void VisitLeaf(
            INamedInputType type, object obj,
            Action<IValueNode> setValue,
            ISet<object> processed)
        {
            if (type is IHasClrType hasClrType)
            {
                if (obj is IOptional optional && !optional.HasValue)
                {
                    return;
                }

                Type currentType = obj.GetType();
                object normalized = currentType == hasClrType.ClrType
                    ? obj
                    : _converter.Convert(currentType, hasClrType.ClrType, obj);

                setValue(type.ParseValue(normalized));
            }
        }
    }
}
