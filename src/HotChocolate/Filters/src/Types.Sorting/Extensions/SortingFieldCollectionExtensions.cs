using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Types.Sorting.Extensions
{
    [Obsolete("Use HotChocolate.Data.")]
    public static class SortingFieldCollectionExtensions
    {
        public static T GetOrAddDescriptor<T>(
            this ICollection<SortOperationDescriptorBase> fields,
            PropertyInfo propertyInfo,
            Func<T> valueFactory) where T : SortOperationDescriptorBase
        {
            if (fields is null)
            {
                throw new ArgumentNullException(nameof(fields));
            }
            if (propertyInfo is null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }
            if (valueFactory is null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            SortOperationDescriptorBase? fieldDescriptor = fields.FirstOrDefault(
                t => t.Definition.Operation?.Property.Equals(propertyInfo) ?? false);

            if (fieldDescriptor is { })
            {
                if (fieldDescriptor is T descritorOfT)
                {
                    return descritorOfT;
                }

                fields.Remove(fieldDescriptor);
            }

            T newDescriptor = valueFactory.Invoke();
            fields.Add(newDescriptor);
            return newDescriptor;
        }
    }
}
