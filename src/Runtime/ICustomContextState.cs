namespace HotChocolate.Runtime
{
    public interface ICustomContextState
    {
        T GetCustomContext<T>();
    }
}
