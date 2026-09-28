#if !NET10_0_OR_GREATER
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Supplies the QUERY members of <see cref="HttpMethods"/> on target frameworks whose
/// ASP.NET Core does not define them.
/// </summary>
internal static class HttpMethodsExtensions
{
    extension(HttpMethods)
    {
        public static string Query => "QUERY";

        public static bool IsQuery(string method)
            => HttpMethods.Equals(HttpMethods.Query, method);
    }
}
#endif
