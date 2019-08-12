using System;

#if ASPNETCLASSIC
using Microsoft.Owin;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Playground
#else
namespace HotChocolate.AspNetCore.Playground
#endif
{
    public class PlaygroundOptions
        : GraphiQLOptionsBase
    {
        public PlaygroundOptions() : base(new PathString("/playground"))
        {
        }
    }
}
