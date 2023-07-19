using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Transport.Http;

public static class GraphQLHttpClientExtensions
{
    public static async Task<GraphQLHttpResponse> GetAsync(
        this IGraphQLHttpClient client,
        string query,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query);
        return await GetAsync(client, operation, cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<GraphQLHttpResponse> GetAsync(
        this IGraphQLHttpClient client,
        string query,
        IReadOnlyDictionary<string, object?>? variables,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query, variables: variables);
        return await GetAsync(client, operation, cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<GraphQLHttpResponse> GetAsync(
        this IGraphQLHttpClient client,
        string query,
        IReadOnlyDictionary<string, object?>? variables = default,
        Uri? uri = default,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }
        
        var operation = new OperationRequest(query, variables: variables);

        return uri is null
            ? await GetAsync(client, operation, cancellationToken).ConfigureAwait(false)
            : await GetAsync(client, operation, uri, cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<GraphQLHttpResponse> GetAsync(
        this IGraphQLHttpClient client,
        string query,
        IReadOnlyDictionary<string, object?>? variables = default,
        string? uri = default,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }
        
        var operation = new OperationRequest(query, variables: variables);

        return uri is null
            ? await GetAsync(client, operation, cancellationToken).ConfigureAwait(false)
            : await GetAsync(client, operation, uri, cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<GraphQLHttpResponse> GetAsync(
        this IGraphQLHttpClient client,
        string query,
        Uri? uri = default,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }
        
        var operation = new OperationRequest(query);

        return uri is null
            ? await GetAsync(client, operation, cancellationToken).ConfigureAwait(false)
            : await GetAsync(client, operation, uri, cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<GraphQLHttpResponse> GetAsync(
        this IGraphQLHttpClient client,
        string query,
        string? uri = default,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }
        
        var operation = new OperationRequest(query);

        return uri is null
            ? await GetAsync(client, operation, cancellationToken).ConfigureAwait(false)
            : await GetAsync(client, operation, uri, cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<GraphQLHttpResponse> GetAsync(
        this IGraphQLHttpClient client,
        OperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }
        
        var request = new GraphQLHttpRequest(operation) { Method = GraphQLHttpMethod.Get };
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    } 
    
    public static async Task<GraphQLHttpResponse> GetAsync(
        this IGraphQLHttpClient client,
        OperationRequest operation,
        string uri,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }
        
        var request = new GraphQLHttpRequest(operation, new Uri(uri)) { Method = GraphQLHttpMethod.Get };
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    } 
    
    public static async Task<GraphQLHttpResponse> GetAsync(
        this IGraphQLHttpClient client,
        OperationRequest operation,
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }
        
        var request = new GraphQLHttpRequest(operation, uri) { Method = GraphQLHttpMethod.Get };
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<GraphQLHttpResponse> PostAsync(
        this IGraphQLHttpClient client,
        string query,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query);
        return await PostAsync(client, operation, cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<GraphQLHttpResponse> PostAsync(
        this IGraphQLHttpClient client,
        string query,
        IReadOnlyDictionary<string, object?>? variables,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query, variables: variables);
        return await PostAsync(client, operation, cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<GraphQLHttpResponse> PostAsync(
        this IGraphQLHttpClient client,
        string query,
        Uri? uri = default,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }
        
        var operation = new OperationRequest(query);

        return uri is null
            ? await PostAsync(client, operation, cancellationToken).ConfigureAwait(false)
            : await PostAsync(client, operation, uri, cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<GraphQLHttpResponse> PostAsync(
        this IGraphQLHttpClient client,
        string query,
        string? uri = default,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }
        
        var operation = new OperationRequest(query);

        return uri is null
            ? await PostAsync(client, operation, cancellationToken).ConfigureAwait(false)
            : await PostAsync(client, operation, uri, cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<GraphQLHttpResponse> PostAsync(
        this IGraphQLHttpClient client,
        string query,
        IReadOnlyDictionary<string, object?>? variables = default,
        Uri? uri = default,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }
        
        var operation = new OperationRequest(query, variables: variables);

        return uri is null
            ? await PostAsync(client, operation, cancellationToken).ConfigureAwait(false)
            : await PostAsync(client, operation, uri, cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<GraphQLHttpResponse> PostAsync(
        this IGraphQLHttpClient client,
        string query,
        IReadOnlyDictionary<string, object?>? variables = default,
        string? uri = default,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }
        
        var operation = new OperationRequest(query, variables: variables);

        return uri is null
            ? await PostAsync(client, operation, cancellationToken).ConfigureAwait(false)
            : await PostAsync(client, operation, uri, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<GraphQLHttpResponse> PostAsync(
        this IGraphQLHttpClient client,
        OperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }
        
        var request = new GraphQLHttpRequest(operation) { Method = GraphQLHttpMethod.Post };
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    } 
    
    public static async Task<GraphQLHttpResponse> PostAsync(
        this IGraphQLHttpClient client,
        OperationRequest operation,
        string uri,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }
        
        var request = new GraphQLHttpRequest(operation, new Uri(uri)) { Method = GraphQLHttpMethod.Post };
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    } 
    
    public static async Task<GraphQLHttpResponse> PostAsync(
        this IGraphQLHttpClient client,
        OperationRequest operation,
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }
        
        var request = new GraphQLHttpRequest(operation, uri) { Method = GraphQLHttpMethod.Post };
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    } 
}