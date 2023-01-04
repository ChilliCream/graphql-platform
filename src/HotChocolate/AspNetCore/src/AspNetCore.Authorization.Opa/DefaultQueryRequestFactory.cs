using System;
using System.Linq;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class DefaultQueryRequestFactory : IOpaQueryRequestFactory
{
    public QueryRequest CreateRequest(AuthorizationContext context, AuthorizeDirective directive)
    {
        var httpContext = (HttpContext)context.ContextData[nameof(HttpContext)];
        var connection = httpContext.Connection;

        var request = new QueryRequest
        {
            Input = new Input
            {
                Policy =
                    new Policy
                    {
                        Path = directive.Policy ?? string.Empty,
                        Roles = directive.Roles is null
                            ? Array.Empty<string>()
                            : directive.Roles.ToArray()
                    },
                Request = new OriginalRequest
                {
                    Headers = httpContext.Request.Headers.ToDictionary(
                        h => h.Key,
                        h => h.Value.ToString()),
                    Host = httpContext.Request.Host.Value,
                    Method = httpContext.Request.Method,
                    Path = httpContext.Request.Path.Value,
                    Query = httpContext.Request.Query,
                    Scheme = httpContext.Request.Scheme
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
