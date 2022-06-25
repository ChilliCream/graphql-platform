using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Common extensions for list filter customization
/// </summary>
public static class ListOperationTypeDescriptorExtensions
{
    /// <summary>
    /// Allows the operation `All` on the type
    /// </summary>
    /// <param name="descriptor">The descriptor</param>
    /// <param name="configure">The configuration of the type of the field</param>
    public static IFilterOperationFieldDescriptor AllowAll<T>(
        this IListOperationTypeDescriptor<T> descriptor,
        Action<IFilterInputTypeDescriptor<T>>? configure = null)
    {
        var operationDescriptor = descriptor.Operation(DefaultFilterOperations.All);
        ConfigureFactory(operationDescriptor, configure);
        return operationDescriptor;
    }

    /// <summary>
    /// Allows the operation `Some` on the type
    /// </summary>
    /// <param name="descriptor">The descriptor</param>
    /// <param name="configure">The configuration of the type of the field</param>
    public static IFilterOperationFieldDescriptor AllowSome<T>(
        this IListOperationTypeDescriptor<T> descriptor,
        Action<IFilterInputTypeDescriptor<T>>? configure = null)
    {
        var operationDescriptor = descriptor.Operation(DefaultFilterOperations.Some);
        ConfigureFactory(operationDescriptor, configure);
        return operationDescriptor;
    }

    /// <summary>
    /// Allows the operation `None` on the type
    /// </summary>
    /// <param name="descriptor">The descriptor</param>
    /// <param name="configure">The configuration of the type of the field</param>
    public static IFilterOperationFieldDescriptor AllowNone<T>(
        this IListOperationTypeDescriptor<T> descriptor,
        Action<IFilterInputTypeDescriptor<T>>? configure = null)
    {
        var operationDescriptor = descriptor.Operation(DefaultFilterOperations.None);
        ConfigureFactory(operationDescriptor, configure);
        return operationDescriptor;
    }

    /// <summary>
    /// Allows the operation `Any` on the type
    /// </summary>
    /// <param name="descriptor">The descriptor</param>
    public static IFilterOperationFieldDescriptor AllowAny<T>(
        this IListOperationTypeDescriptor<T> descriptor)
        => descriptor.Operation(DefaultFilterOperations.Any);

    private static void ConfigureFactory<T>(
        IFilterOperationFieldDescriptor descriptor,
        Action<IFilterInputTypeDescriptor<T>>? configure = null)
    {
        if (configure is null)
        {
            return;
        }

        descriptor.Extend().Definition.CreateFieldTypeDefinition = CreateFieldTypeDefinition;

        FilterInputTypeDefinition CreateFieldTypeDefinition(
            IDescriptorContext context,
            string? scope)
        {
            var d = FilterInputTypeDescriptor.Inline<T>(context, typeof(T), scope);

            configure(d);
            return d.CreateDefinition();
        }
    }
}
