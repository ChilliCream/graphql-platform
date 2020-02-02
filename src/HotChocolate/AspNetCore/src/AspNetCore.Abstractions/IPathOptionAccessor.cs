using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public interface IPathOptionAccessor
    {
        PathString Path { get; }
    }
}
