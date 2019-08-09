using System;

#if ASPNETCLASSIC
using Microsoft.Owin;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.GraphiQL
#else
namespace HotChocolate.AspNetCore.GraphiQL
#endif
{
    public class GraphiQLOptions
        : GraphiQLOptionsBase
    {
        public GraphiQLOptions() : base(new PathString("/graphiql"))
        {
        }
    }
}
