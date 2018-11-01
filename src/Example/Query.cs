using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Subscriptions;

namespace Example
{
    public class Query
    {
        public string Hello() => "world";
    }

    public class Mutation
    {
        public async Task<string> DoSomething([Service]IEventSender eventSender)
        {
            await eventSender.SendAsync(new EventMessage("onDoSomething"));
            return "done";
        }
    }

    public class Subscription
    {
        public string OnDoSomething()
        {
            return "foo";
        }
    }
}
