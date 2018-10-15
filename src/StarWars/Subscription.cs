using System.Threading.Tasks;

namespace StarWars
{
    public class Subscription
    {
        public Task<string> Foo()
        {
            return Task.FromResult("foo");
        }
    }
}
