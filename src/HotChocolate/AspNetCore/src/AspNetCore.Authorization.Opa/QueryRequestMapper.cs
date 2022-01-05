using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public static class QueryRequestMapper
{
    public static QueryRequest MapFrom(IMiddlewareContext context, AuthorizeDirective directive)
    {
        IHttpContextAccessor? accessor = context.Services.GetService<IHttpContextAccessor>();
        HttpContext? http = accessor.HttpContext;
        ConnectionInfo? connection = http.Connection;

        var request = new QueryRequest
        {
            Input = new Input
            {
                GraphQL = new GraphQl
                {
                    Policy = directive.Policy ?? string.Empty,
                    Roles = directive.Roles is null ? Array.Empty<string>() : directive.Roles.ToArray()
                },
                Request = new OriginalRequest
                {
                    Headers = http.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                    Host = http.Request.Host.Value,
                    Method = http.Request.Method,
                    Path = http.Request.Path.Value,
                    Query = http.Request.Query,
                    Scheme = http.Request.Scheme
                },
                Source = new IPAndPort
                {
                    IpAddress = connection.RemoteIpAddress.ToString(),
                    Port = connection.RemotePort
                },
                Destination = new IPAndPort
                {
                    IpAddress = connection.LocalIpAddress.ToString(),
                    Port = connection.LocalPort
                }
            }
        };
        return request;
    }
}
