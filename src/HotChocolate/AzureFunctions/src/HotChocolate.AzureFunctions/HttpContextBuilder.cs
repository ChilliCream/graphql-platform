using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
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
        IEnumerable<ClaimsIdentity>? claimsIdentities = null
    )
    {
        //Initialize the root Http Context (Container)...
        var httpContext = new DefaultHttpContext();

        //Initialize the Http Request...
        HttpRequest httpRequest = httpContext.Request;
        httpRequest.Scheme = requestUri.Scheme;
        httpRequest.Path = new PathString(requestUri.AbsolutePath);
        httpRequest.Method = requestHttpMethod ?? HttpMethod.Post.Method;
        httpRequest.QueryString = new QueryString(requestUri.Query);

        //Ensure we marshall across all Headers from the Client Request...
        if (requestHeaders?.Any() == true)
            foreach (var header in requestHeaders)
                httpRequest.Headers.Add(header.Key, new StringValues(header.Value.ToArray()));

        if (!string.IsNullOrEmpty(requestBody))
        {
            //Initialize a valid Stream for the Request (must be tracked & Disposed of!)
            var requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);
            httpRequest.Body = new MemoryStream(requestBodyBytes);
            httpRequest.ContentType = requestBodyContentType;
            httpRequest.ContentLength = requestBodyBytes.Length;
        }

        //Initialize the Http Response...
        HttpResponse httpResponse = httpContext.Response;
        //Initialize a valid Stream for the Response (must be tracked & Disposed of!)
        //NOTE: Default Body is a NullStream...which ignores all Reads/Writes.
        httpResponse.Body = new MemoryStream();

        //Proxy over any possible authentication claims if available
        if (claimsIdentities?.Any() == true)
            httpContext.User = new ClaimsPrincipal(claimsIdentities);

        return httpContext;
    }
}
