using Microsoft.Owin;

namespace HotChocolate.AspNetClassic
{
    public interface IOwinContextAccessor
    {
        IOwinContext OwinContext { get; }
    }
}
