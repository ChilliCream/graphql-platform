using System;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Provides extension methods to the <see cref="IFilterOperationFieldDescriptor"/>
/// </summary>
public static class RelayIdFilterFieldExtensions
{
    /// <summary>
    /// Makes the operation field type an ID type.
    /// </summary>
    /// <param name="descriptor">
    /// The filter operation field descriptor.
    /// </param>
    /// <returns>
    /// Returns the filter operation field descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IFilterOperationFieldDescriptor ID(
        this IFilterOperationFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor
            .Extend()
            .OnBeforeCompletion((c, d) =>
            {
                d.Formatters.Push(CreateSerializer(c));
            });

        return descriptor;
    }

    private static IInputValueFormatter CreateSerializer(
        ITypeCompletionContext completionContext)
        => new FilterGlobalIdInputValueFormatter(
            completionContext.DescriptorContext.NodeIdSerializerAccessor);
}
