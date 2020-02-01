using System.Threading.Tasks;

namespace StrawberryShake.Transport
{
    public interface ISocketConnectionInterceptor
    {
        Task OnConnectAsync(ISocketConnection connection);

        Task OnDisconnectAsync(ISocketConnection connection);
    }
}
