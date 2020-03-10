namespace HotChocolate.Execution
{
    internal sealed class ResourceManager
    {
        /*
while(resolvers.Count > 0)
{
    bool waitForResource = true;

    for(int i = 0; i < resolvers.Count; i++)
    {
        ResolverContext context = resolvers.Dequeue();
        if(resourceManager.TryEnqueue(context))
        {
            waitForResource = false;
        }
        else
        {
            resolvers.Enqueue(context);
        }
    }

    if(waitForResource)
    {
        await resourceManager.WaitForResourceAsync();
    }
}

await resourceManager.CompleteAsync();
        */
    }
}
