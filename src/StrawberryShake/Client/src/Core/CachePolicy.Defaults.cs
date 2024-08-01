using StrawberryShake.Extensions;

namespace StrawberryShake;

public partial class CachePolicy
{
    public static CachePolicy Default(IStoreAccessor storeAccessor) =>
        Default(storeAccessor, TimeSpan.FromMinutes(5));

    public static CachePolicy Default(IStoreAccessor storeAccessor, TimeSpan timeToLive)
    {
        var sync = new object();
        var lastClean = DateTime.UtcNow;
        var cleaning = false;

        return new(storeAccessor.OperationStore.Watch().Subscribe(result =>
        {
            var time = DateTime.UtcNow;

            if (!cleaning && time - lastClean >= timeToLive)
            {
                lock (sync)
                {
                    if (!cleaning && time - lastClean >= timeToLive)
                    {
                        Clean(time);
                    }
                }
            }
        }));

        void Clean(DateTime time)
        {
            cleaning = true;

            try
            {
                foreach (var operationVersion in storeAccessor.OperationStore.GetAll())
                {
                    if (operationVersion.Subscribers == 0 &&
                        time - operationVersion.LastModified >= timeToLive)
                    {
                        storeAccessor.OperationStore.Remove(operationVersion.Request);
                    }
                }
            }
            finally
            {
                lastClean = DateTime.UtcNow;
                cleaning = false;
            }
        }
    }

    public static CachePolicy NoCache(IStoreAccessor storeAccessor) =>
        new(storeAccessor.OperationStore.Watch().Subscribe(result =>
        {
            if (result.Kind == OperationUpdateKind.Updated)
            {
                foreach (var operationVersion in result.OperationVersions)
                {
                    if (operationVersion.Subscribers == 0)
                    {
                        storeAccessor.OperationStore.Remove(operationVersion.Request);
                    }
                }
            }
        }));
}
