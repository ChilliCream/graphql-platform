using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    public interface IEventSender
    {
        Task SendAsync(Event @event);
    }
}

