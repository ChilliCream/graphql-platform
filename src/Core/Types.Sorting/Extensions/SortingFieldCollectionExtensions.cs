using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace HotChocolate.Types.Sorting.Extensions
{
    public static class SortingFieldCollectionExtensions
    {
        public static T GetOrAddDescriptor<T>(
            this ICollection<SortOperationDescriptor> fields,
            PropertyInfo propertyInfo,
            Func<T> valueFactory) where T : SortOperationDescriptor
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }
            if (propertyInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }
            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            SortOperationDescriptor fieldDescriptor =
                fields.FirstOrDefault(t => t.Definition.Operation.Property.Equals(propertyInfo));

            if (fieldDescriptor is { })
            {
                if (fieldDescriptor is T descritorOfT)
                {
                    return descritorOfT;
                }
                else
                {
                    fields.Remove(fieldDescriptor);
                }
            }

            T newDescirptor = valueFactory.Invoke();
            fields.Add(newDescirptor);
            return newDescirptor;
        }
    }
}
