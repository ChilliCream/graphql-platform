namespace Zeus
{
    public interface INodeResolver
    {
        int Depth { get; }

        IResolver Resolver();
    }


}
