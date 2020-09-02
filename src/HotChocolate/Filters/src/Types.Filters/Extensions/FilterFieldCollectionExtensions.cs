using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Types.Filters.Properties;

namespace HotChocolate.Types.Filters.Extensions
{
    public static class FilterFieldCollectionExtensions
    {
        public static T GetOrAddDescriptor<T>(
            this IList<FilterFieldDescriptorBase> fields,
            PropertyInfo propertyInfo,
            Func<T> valueFactory) where T : FilterFieldDescriptorBase
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

            FilterFieldDescriptorBase fieldDescriptor =
                fields.FirstOrDefault(t => t.Definition.Property.Equals(propertyInfo));

            if (fieldDescriptor is { })
            {
                if (fieldDescriptor is T descriptorOfT)
                {
                    return descriptorOfT;
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
            if (fields is null)
            {
                throw new ArgumentNullException(nameof(fields));
            }
            if (valueFactory is null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            FilterOperationDescriptorBase operationDescriptor =
                fields.FirstOrDefault(
                    t => t.Definition.Operation?.Kind.Equals(operationKind) == true);

            if (operationDescriptor is { })
            {
                if (operationDescriptor is T descriptorOfT)
                {
                    return descriptorOfT;
                }
                else
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                        .SetMessage(
                            string.Format(
                               FilterResources.FilterFieldOperationDescriptor_InvalidDescriptorType
                               , operationDescriptor.GetType().Name
                               , typeof(T).Name))
                        .SetCode(ErrorCodes.Filtering.FilterFieldDescriptorType)
                        .Build());

                }
            }

            T newDescirptor = valueFactory.Invoke();
            fields.Add(newDescirptor);
            return newDescirptor;
        }
    }
}
