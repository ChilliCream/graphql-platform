namespace Mocha;

internal sealed class MessageTypePostConfigureConvention : IMessageTypeConfigurationConvention
{
    public void Configure(IMessagingConfigurationContext context, MessageTypeConfiguration configuration)
    {
        if (configuration is { Identity: null, RuntimeType: not null })
        {
            configuration.Identity = context.Naming.GetMessageIdentity(configuration.RuntimeType);
        }
    }

    public static readonly IConvention Instance = new MessageTypePostConfigureConvention();
}
