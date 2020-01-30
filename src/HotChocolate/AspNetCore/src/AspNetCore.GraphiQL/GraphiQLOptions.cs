using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.GraphiQL
{
    public class GraphiQLOptions
        : GraphiQLOptionsBase
    {
        public GraphiQLOptions() : base(new PathString("/graphiql"))
        {
        }
    }
}
