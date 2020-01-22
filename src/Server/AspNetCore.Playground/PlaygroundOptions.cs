using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Playground
{
    public class PlaygroundOptions
        : GraphiQLOptionsBase
    {
        public PlaygroundOptions() : base(new PathString("/playground"))
        {
        }
    }
}
