using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal class InputObjectToDictionaryConverter
    {
        private readonly ITypeConversion _converter;

        public InputObjectToDictionaryConverter(ITypeConversion converter)
        {
            _converter = converter
                ?? throw new ArgumentNullException(nameof(converter));
        }

        public Dictionary<string, object> Convert(
            InputObjectType type, object obj)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            Dictionary<string, object> dict = null;
            Action<object> setValue =
                value => dict = (Dictionary<string, object>)value;
            VisitInputObject(type, obj, setValue, new HashSet<object>());
            return dict;
        }

        private void VisitValue(
            IInputType type, object obj,
            Action<object> setValue,
            ISet<object> processed)
        {
            if (obj is null)
            {
                setValue(null);
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
            Action<object> setValue, ISet<object> processed)
        {
            if (processed.Add(obj))
            {
                var dict = new Dictionary<string, object>();
                setValue(dict);

                foreach (InputField field in type.Fields)
                {
                    object fieldValue = field.GetValue(obj);
                    Action<object> setField = value => dict[field.Name] = value;
                    VisitValue(field.Type, fieldValue, setField, processed);
                }
            }
        }

        private void VisitList(
            ListType type, object obj,
            Action<object> setValue, ISet<object> processed)
        {
            if (obj is IEnumerable sourceList)
            {
                var list = new List<object>();
                setValue(list);

                var itemType = (IInputType)type.ElementType;
                Action<object> addItem = item => list.Add(item);

                foreach (object item in sourceList)
                {
                    VisitValue(itemType, item, addItem, processed);
                }
            }
        }

        private void VisitLeaf(
            INamedInputType type, object obj,
            Action<object> setValue, ISet<object> processed)
        {
            if (type is IHasClrType hasClrType)
            {
                Type currentType = obj.GetType();
                object normalized = currentType == hasClrType.ClrType
                    ? obj
                    : _converter.Convert(currentType, hasClrType.ClrType, obj);
                setValue(obj);
            }
        }
    }
}
