namespace Mocha;

internal sealed class LazyMessagingRuntime : ILazyMessagingRuntime
{
    public IMessagingRuntime Runtime
    {
        get
        {
            if (field is null)
            {
                throw ThrowHelper.MessagingRuntimeNotInitialized();
            }

            return field;
        }
        set;
    }
}
