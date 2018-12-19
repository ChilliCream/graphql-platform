using System;
using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.Utilities
{
    public partial class TypeConversion
    {
        private bool TryCreateListTypeConverter(Type from, Type to,
            out ChangeType listConverter)
        {
            Type fromElement = DotNetTypeInfoFactory.GetInnerListType(from);
            Type toElement = DotNetTypeInfoFactory.GetInnerListType(to);

            if (fromElement != null && toElement != null
                && TryGetOrCreateConverter(fromElement, toElement,
                    out ChangeType converter))
            {
                if (to.IsArray)
                {
                    listConverter = source => GenericArrayConverter(
                        toElement, (ICollection)source, converter);
                    Register(from, to, listConverter);
                }
                else
                {
                    Type listType = to.IsInterface
                        ? typeof(List<>).MakeGenericType(toElement)
                        : to;
                    listConverter = source => GenericListConverter(
                        listType, (ICollection)source, converter);
                    Register(from, to, listConverter);
                }

                return true;
            }

            listConverter = null;
            return false;
        }

        private static void ChangeListType(IEnumerable source,
            Action<object, int> addToDestination)
        {
            int i = 0;
            foreach (object item in source)
            {
                addToDestination(item, i++);
            }
        }

        private static object GenericArrayConverter(
            Type elementType, ICollection source,
            ChangeType converter)
        {
            Array array = Array.CreateInstance(elementType, source.Count);
            ChangeListType(source, (item, index) =>
                array.SetValue(converter(item), index));
            return array;
        }

        private static object GenericListConverter(
            Type listType, ICollection source,
            ChangeType converter)
        {
            IList list = (IList)Activator.CreateInstance(listType);
            ChangeListType(source, (item, index) =>
                list.Add(converter(item)));
            return list;
        }
    }
}
