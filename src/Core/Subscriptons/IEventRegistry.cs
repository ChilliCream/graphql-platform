using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    public interface IEventRegistry
    {
        Task<IEventStream> SubscribeAsync(Event @event);
    }
}

