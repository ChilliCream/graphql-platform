using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AzureFunctions;

public class HttpContextBuilder
{
    public virtual HttpContext CreateHttpContext(
        string requestHttpMethod,
        Uri requestUri,
        string? requestBody = null,
        string requestBodyContentType = GraphQLAzureFunctionsConstants.DefaultJsonContentType,
        HttpHeaders? requestHeaders = null,
        IEnumerable<ClaimsIdentity>? claimsIdentities = null,
        IDictionary<object, object>? contextItems = null
    )
    {
        //Initialize the root Http Context (Container)...
        var httpContext = new DefaultHttpContext();

        //Initialize the Http Request...
        var httpRequest = httpContext.Request;
        httpRequest.Method = requestHttpMethod ?? throw new ArgumentNullException(nameof(requestHttpMethod));
        httpRequest.Scheme = requestUri?.Scheme ?? throw new ArgumentNullException(nameof(requestUri));
        httpRequest.Host = new HostString(requestUri.Host, requestUri.Port);
        httpRequest.Path = new PathString(requestUri.AbsolutePath);
        httpRequest.QueryString = new QueryString(requestUri.Query);

        //Ensure we marshall across all Headers from the Client Request...
        //Note: This should also handle Cookies since Cookies are stored as a Header value....
        if (requestHeaders?.Any() == true)
            foreach (var (key, value) in requestHeaders)
                httpRequest.Headers.TryAdd(key, new StringValues(value.ToArray()));

        if (!string.IsNullOrEmpty(requestBody))
        {
            //Initialize a valid Stream for the Request (must be tracked & Disposed of!)
            var requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);
            httpRequest.Body = new MemoryStream(requestBodyBytes);
            httpRequest.ContentType = requestBodyContentType;
            httpRequest.ContentLength = requestBodyBytes.Length;
        }

        //Initialize the Http Response...
        var httpResponse = httpContext.Response;
        //Initialize a valid Stream for the Response (must be tracked & Disposed of!)
        //NOTE: Default Body is a NullStream...which ignores all Reads/Writes.
        httpResponse.Body = new MemoryStream();

        //Proxy over any possible authentication claims if available
        if (claimsIdentities?.Any() == true)
            httpContext.User = new ClaimsPrincipal(claimsIdentities);

        //Set the Custom Context Items if specified...
        if(contextItems?.Any() == true)
            foreach (var item in contextItems)
                httpContext.Items.TryAdd(item.Key, item.Value);

        return httpContext;
    }
}
