namespace HotChocolate.Server
{
    public interface ISocketSessionFactory
    {
        ISocketSession Create(
            ISocketConnection connection,
            IQueryRequestFactory requestFactory);
    }
}
