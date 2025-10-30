using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

internal static class DescriptorAttributeHelper
{
    public static void ApplyConfiguration(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? element)
    {
        if (element is null)
        {
            return;
        }

        var count = 0;
        var attributes = context.TypeInspector.GetAttributes(element, true);
        IDescriptorConfiguration? first = null;
        IDescriptorConfiguration[]? configurations = null;

        try
        {
            foreach (var attribute in attributes)
            {
                if (attribute is IDescriptorConfiguration casted)
                {
                    if (configurations is null && first == null)
                    {
                        first = casted;
                    }
                    else if (configurations is null && first is not null)
                    {
                        configurations = ArrayPool<IDescriptorConfiguration>.Shared.Rent(attributes.Length);
                        configurations[count++] = first;
                        configurations[count++] = casted;
                        first = null;
                    }
                    else
                    {
                        configurations?[count++] = casted;
                    }
                }
            }

            if (first is not null)
            {
                first.TryConfigure(context, descriptor, element);
                return;
            }

            if (configurations is null)
            {
                return;
            }

            var configurationSpan = configurations.AsSpan(0, count);
            configurationSpan.Sort(DescriptorAttributeComparer.Default);

            foreach (var configuration in configurationSpan)
            {
                configuration.TryConfigure(context, descriptor, element);
            }
        }
        finally
        {
            if (configurations is not null)
            {
                configurations.AsSpan(0, count).Clear();
                ArrayPool<IDescriptorConfiguration>.Shared.Return(configurations);
            }
        }
    }

    private sealed class DescriptorAttributeComparer : IComparer<IDescriptorConfiguration>
    {
        public static DescriptorAttributeComparer Default { get; } = new();

        public int Compare(IDescriptorConfiguration? x, IDescriptorConfiguration? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (y is null)
            {
                return 1;
            }

            if (x is null)
            {
                return -1;
            }

            return x.Order.CompareTo(y.Order);
        }
    }
}
