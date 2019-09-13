namespace HotChocolate.Server
{
    public interface ISocketQueryRequestInterceptor
        : IQueryRequestInterceptor<ISocketConnection>
    {
    }
}
