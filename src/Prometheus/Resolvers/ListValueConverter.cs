using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Prometheus.Abstractions;

namespace Prometheus.Resolvers
{
    internal class ListValueConverter
        : IValueConverter
    {
        public object Convert(IValue value, Type desiredType)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is ListValue lv)
            {
                if (desiredType.IsGenericType)
                {
                    Type elementType = desiredType.GetGenericArguments().Single();
                    Type listType = typeof(List<>).MakeGenericType(elementType);

                    if (desiredType.IsAssignableFrom(listType))
                    {
                        return CreateList(lv, elementType);
                    }

                    Type setType = typeof(HashSet<>).MakeGenericType(elementType);
                    if (desiredType.IsAssignableFrom(setType))
                    {
                        return CreateSet(lv, elementType);
                    }
                }
                else if (desiredType.IsArray)
                {
                    Type elementType = desiredType.GetElementType();
                    return CreateArray(lv, elementType);
                }
                else if (desiredType == typeof(object))
                {
                    Type listType = typeof(List<>).MakeGenericType(typeof(object));
                    if (desiredType.IsAssignableFrom(listType))
                    {
                        return CreateList(lv, typeof(object));
                    }
                }

                throw new NotSupportedException();
            }

            throw new ArgumentException(
                "The specified type is not an list type.",
                nameof(value));
        }

        private object CreateList(ListValue listValue, Type elementType)
        {
            Type genericListType = typeof(List<>);
            Type listType = genericListType.MakeGenericType(elementType);

            IList list = (IList)Activator.CreateInstance(listType);

            foreach (IValue item in listValue.Items)
            {
                list.Add(ValueConverter.Convert(item, elementType));
            }

            return list;
        }

        private object CreateArray(ListValue listValue, Type elementType)
        {
            Array array = Array.CreateInstance(elementType, listValue.Items.Count);
            int i = 0;

            foreach (IValue value in listValue.Items)
            {
                object element = ValueConverter.Convert(value, elementType);
                array.SetValue(element, i++);
            }

            return array;
        }

        private object CreateSet(ListValue listValue, Type elementType)
        {
            Type genericListType = typeof(HashSet<>);
            Type setType = genericListType.MakeGenericType(elementType);
            MethodInfo addMethod = setType.GetMethod("Add");

            object set = Activator.CreateInstance(setType);
            Action<object> addElement = element =>
            {
                addMethod.Invoke(set, new[] { element });
            };

            foreach (IValue item in listValue.Items)
            {
                addElement(ValueConverter.Convert(item, elementType));
            }

            return set;
        }

        public IValue Convert(object value, ISchemaDocument schema, IType desiredType)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (desiredType == null)
            {
                throw new ArgumentNullException(nameof(desiredType));
            }

            if (value == null)
            {
                return NullValue.Instance;
            }

            IType elementType = desiredType.ElementType();
            IValue[] elements = ((IEnumerable)value).OfType<object>()
                .Select(t => ValueConverter.Convert(value, schema, elementType))
                .ToArray();
            return new ListValue(elements);
        }
    }
}