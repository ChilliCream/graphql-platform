using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace HotChocolate.Types.Filters.Extensions
{
    public static class FilterFieldCollectionExtensions
    {
        public static T GetOrAddDescriptor<T>(
            this IList<FilterFieldDescriptorBase> fields,
            PropertyInfo propertyInfo,
            Func<T> valueFactory) where T : FilterFieldDescriptorBase
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

            FilterFieldDescriptorBase fieldDescriptor =
                fields.FirstOrDefault(t => t.Definition.Property.Equals(propertyInfo));

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

        public static T GetOrAddOperation<T>(
            this ICollection<FilterOperationDescriptorBase> fields,
            FilterOperationKind operationKind,
            Func<T> valueFactory) where T : FilterOperationDescriptorBase
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }
            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            FilterOperationDescriptorBase operationDescriptor =
                fields.FirstOrDefault(t => t.Definition.Operation.Kind.Equals(operationKind));

            if (operationDescriptor is { })
            {
                if (operationDescriptor is T descritorOfT)
                {
                    return descritorOfT;
                }
                else
                {
                    fields.Remove(operationDescriptor);
                }
            }

            T newDescirptor = valueFactory.Invoke();
            fields.Add(newDescirptor);
            return newDescirptor;
        }
    }
}
