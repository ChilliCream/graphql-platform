using System;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters;

internal static class RelayIdFilterFieldExtensions
{
    private static IdSerializer? _idSerializer;

    internal static IFilterOperationFieldDescriptor ID(
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
    {
        var serializer =
            completionContext.Services.GetService<IIdSerializer>() ??
            (_idSerializer ??= new IdSerializer());

        return new FilterGlobalIdInputValueFormatter(serializer);
    }
}
