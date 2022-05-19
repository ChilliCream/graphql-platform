using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters;

/// <summary>
///  Common extension of the <see cref="IFilterInputTypeDescriptor"/> for list filtering
/// </summary>
public static class ListFilterDescriptorExtensions
{
    /// <summary>
    /// Defines a <see cref="FilterField" /> that binds to the specified property and also
    /// configures the type of the field
    /// </summary>
    /// <param name="descriptor">The descriptor</param>
    /// <param name="propertyOrMember">
    /// The property to which a filter field shall be bound.
    /// </param>
    /// <param name="configure">The configuration of the type of the field</param>
    public static IFilterFieldDescriptor Field<T, TField>(
        this IFilterInputTypeDescriptor<T> descriptor,
        Expression<Func<T, IEnumerable<TField?>?>> propertyOrMember,
        Action<IListOperationTypeDescriptor<TField>> configure)
    {
        IFilterFieldDescriptor fieldDescriptor = descriptor.Field(propertyOrMember);
        fieldDescriptor.Extend().Definition.CreateFieldTypeDefinition = CreateFieldTypeDefinition;
        return fieldDescriptor;

        FilterInputTypeDefinition CreateFieldTypeDefinition(
            IDescriptorContext context,
            string? scope)
        {
            var typeDescriptor =
                FilterInputTypeDescriptor.Inline<TField>(context, typeof(object), scope);
            var listDescriptor = new ListOperationTypeDescriptor<TField>(typeDescriptor);
            configure(listDescriptor);
            return typeDescriptor.CreateDefinition();
        }
    }
}
