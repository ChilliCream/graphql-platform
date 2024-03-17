using System.Threading.Tasks;

namespace GreenDonut;

public sealed class DelayDispatcher : IBatchScheduler
{
    public void Schedule(BatchJob job)
        => Task.Run(async () =>
        {
            await Task.Delay(150);
            await job.DispatchAsync();
        });
}