
#if ASPNETCLASSIC
using Microsoft.Owin;
using HotChocolate.AspNetClassic.Interceptors;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public interface IPathOptionAccessor
    {
        PathString Path { get; }
    }
}
