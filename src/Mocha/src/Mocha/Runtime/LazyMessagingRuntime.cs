namespace Mocha;

internal sealed class LazyMessagingRuntime : ILazyMessagingRuntime
{
    public IMessagingRuntime Runtime
    {
        get
        {
            if (field is null)
            {
                throw new InvalidOperationException(
                    "Messaging runtime is not initialized, you can only access the runtime after it has been built.");
            }

            return field;
        }
        set;
    }
}
