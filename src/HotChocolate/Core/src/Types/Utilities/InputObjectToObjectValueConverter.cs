using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal class InputObjectToObjectValueConverter
    {
        private readonly ITypeConverter _converter;

        public InputObjectToObjectValueConverter(ITypeConverter converter)
        {
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        public ObjectValueNode Convert(
            InputObjectType type,
            object obj)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            ObjectValueNode objectValueNode = null;
            void SetValue(IValueNode value) => objectValueNode = (ObjectValueNode)value;
            VisitInputObject(type, obj, SetValue, new HashSet<object>());
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
                VisitList(type.ListType(), obj, setValue, processed);
            }
            else if (type.IsLeafType())
            {
                VisitLeaf((INamedInputType)type.NamedType(), obj, setValue);
            }
            else if (type.IsInputObjectType())
            {
                VisitInputObject((InputObjectType)type.NamedType(), obj, setValue, processed);
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
            if (processed.Add(obj))
            {
                var fields = new List<ObjectFieldNode>();

                foreach (InputField field in type.Fields)
                {
                    if(field.TryGetValue(obj, out object fieldValue))
                    {
                        void SetField(IValueNode value) =>
                            fields.Add(new ObjectFieldNode(field.Name, value));
                        VisitValue(field.Type, fieldValue, SetField, processed);
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
            if (obj is IEnumerable sourceList)
            {
                var list = new List<IValueNode>();
                var itemType = (IInputType)type.ElementType;
                void AddItem(IValueNode item) => list.Add(item);

                foreach (object item in sourceList)
                {
                    VisitValue(itemType, item, AddItem, processed);
                }

                setValue(new ListValueNode(list));
            }
        }

        private void VisitLeaf(
            INamedInputType type, object obj,
            Action<IValueNode> setValue)
        {
            if (type is IHasRuntimeType hasClrType)
            {
                Type currentType = obj.GetType();
                object normalized = currentType == hasClrType.RuntimeType
                    ? obj
                    : _converter.Convert(currentType, hasClrType.RuntimeType, obj);

                setValue(type.ParseValue(normalized));
            }
        }
    }
}
