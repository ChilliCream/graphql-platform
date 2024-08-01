namespace GreenDonut;

public class DelayDispatcher : IBatchScheduler
{
    public void Schedule(Func<ValueTask> dispatch)
        => Task.Run(async () =>
        {
            await Task.Delay(150);
            await dispatch();
        });
}
