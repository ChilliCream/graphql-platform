using Mocha.Features;

namespace Mocha;

internal static class MessagingConfigurationContextExtensions
{
    extension(IMessagingConfigurationContext context)
    {
        internal MessagingConfigurationContainer Configurations
            => context.Features.GetRequired<MessagingConfigurationFeature>().Configurations;

        internal void ApplyConfigurations<TDescriptor>(Type runtimeType, TDescriptor descriptor)
            where TDescriptor : IMessagingDescriptor
        {
            if (context.Features.Get<MessagingConfigurationFeature>() is { } feature)
            {
                feature.Configurations.Apply(runtimeType, descriptor);
            }
        }
    }
}
