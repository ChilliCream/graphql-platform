namespace GreenDonut;

public class DelayDispatcher : IBatchScheduler
{
    public void Schedule(Batch batch)
        => Task.Run(async () =>
        {
            await Task.Delay(150);
            await batch.DispatchAsync();
        });
}
