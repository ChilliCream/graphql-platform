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
        ICustomAttributeProvider? element,
        ICustomAttributeProvider? mainAttributeProvider,
        ImmutableList<IDescriptorConfiguration> additionalConfigurations)
    {
        if (element is null
            || ((mainAttributeProvider is null || ReferenceEquals(mainAttributeProvider, typeof(object)))
                && additionalConfigurations.Count == 0))
        {
            return;
        }

        var configurations = ArrayPool<IDescriptorConfiguration>.Shared.Rent(64);
        var count = 0;

        if (mainAttributeProvider is not null && !ReferenceEquals(mainAttributeProvider, typeof(object)))
        {
            CollectAttributeConfigurations(
                context.TypeInspector,
                mainAttributeProvider,
                ref configurations,
                ref count);
        }

        if (additionalConfigurations.Count > 0)
        {
            EnsureCapacity(additionalConfigurations.Count, ref configurations, ref count);

            foreach (var configuration in additionalConfigurations)
            {
                configurations[count++] = configuration;
            }
        }

        if (count == 0)
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

    private static void CollectAttributeConfigurations(
        ITypeInspector typeInspector,
        ICustomAttributeProvider attributeProvider,
        ref IDescriptorConfiguration[] configurations,
        ref int count)
    {
        var i = count;
        var attributes = typeInspector.GetAttributes(attributeProvider, true);

        EnsureCapacity(attributes.Length, ref configurations, ref count);

        foreach (var attribute in attributes)
        {
            if (attribute is IDescriptorConfiguration casted)
            {
                configurations[i++] = casted;
            }
        }

        count = i;
    }

    private static void EnsureCapacity(
        int requiredCapacity,
        ref IDescriptorConfiguration[] configurations,
        ref int count)
    {
        if (count + requiredCapacity > configurations.Length)
        {
            var requiredSize = Math.Max(count * 2, count + requiredCapacity);
            var temp = ArrayPool<IDescriptorConfiguration>.Shared.Rent(requiredSize);
            var configurationsSpan = configurations.AsSpan(0, count);

            configurationsSpan.CopyTo(temp);
            configurationsSpan.Clear();

            ArrayPool<IDescriptorConfiguration>.Shared.Return(configurations);
            configurations = temp;
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
