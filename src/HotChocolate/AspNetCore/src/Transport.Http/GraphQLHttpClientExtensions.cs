using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Transport.Http;

/// <summary>
/// Provides extension methods for <see cref="GraphQLHttpClient"/>.
/// </summary>
public static class GraphQLHttpClientExtensions
{
    /// <summary>
    /// Sends a GraphQL GET request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> GetAsync(
        this GraphQLHttpClient client,
        string query,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query);
        return GetAsync(client, operation, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL GET request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="variables">
    /// The GraphQL variables.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> GetAsync(
        this GraphQLHttpClient client,
        string query,
        IReadOnlyDictionary<string, object?>? variables,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query, variables: variables);
        return GetAsync(client, operation, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL GET request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="variables">
    /// The GraphQL variables.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> GetAsync(
        this GraphQLHttpClient client,
        string query,
        IReadOnlyDictionary<string, object?>? variables = default,
        Uri? uri = default,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query, variables: variables);
        return uri is null
            ? GetAsync(client, operation, cancellationToken)
            : GetAsync(client, operation, uri, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL GET request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="variables">
    /// The GraphQL variables.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> GetAsync(
        this GraphQLHttpClient client,
        string query,
        IReadOnlyDictionary<string, object?>? variables = default,
        string? uri = default,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query, variables: variables);
        return uri is null
            ? GetAsync(client, operation, cancellationToken)
            : GetAsync(client, operation, uri, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL GET request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> GetAsync(
        this GraphQLHttpClient client,
        string query,
        Uri? uri = default,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query);
        return uri is null
            ? GetAsync(client, operation, cancellationToken)
            : GetAsync(client, operation, uri, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL GET request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> GetAsync(
        this GraphQLHttpClient client,
        string query,
        string? uri = default,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query);
        return uri is null
            ? GetAsync(client, operation, cancellationToken)
            : GetAsync(client, operation, uri, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL GET request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="operation">
    /// The <see cref="OperationRequest"/> to send.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> GetAsync(
        this GraphQLHttpClient client,
        OperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        var request = new GraphQLHttpRequest(operation)
        {
            Method = GraphQLHttpMethod.Get,
        };

        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL GET request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="operation">
    /// The <see cref="OperationRequest"/> to send.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> GetAsync(
        this GraphQLHttpClient client,
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

        var request = new GraphQLHttpRequest(operation, new Uri(uri))
        {
            Method = GraphQLHttpMethod.Get,
        };

        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL GET request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="operation">
    /// The <see cref="OperationRequest"/> to send.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> GetAsync(
        this GraphQLHttpClient client,
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

        var request = new GraphQLHttpRequest(operation, uri)
        {
            Method = GraphQLHttpMethod.Get,
        };

        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        string query,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query);
        return PostAsync(client, operation, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="variables">
    /// The GraphQL variables.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        string query,
        IReadOnlyDictionary<string, object?>? variables,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query, variables: variables);
        return PostAsync(client, operation, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        string query,
        Uri? uri = default,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query);
        return uri is null
            ? PostAsync(client, operation, cancellationToken)
            : PostAsync(client, operation, uri, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        string query,
        string? uri = default,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query);
        return uri is null
            ? PostAsync(client, operation, cancellationToken)
            : PostAsync(client, operation, uri, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="variables">
    /// The GraphQL variables.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        string query,
        IReadOnlyDictionary<string, object?>? variables = default,
        Uri? uri = default,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query, variables: variables);
        return uri is null
            ? PostAsync(client, operation, cancellationToken)
            : PostAsync(client, operation, uri, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="variables">
    /// The GraphQL variables.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        string query,
        IReadOnlyDictionary<string, object?>? variables = default,
        string? uri = default,
        CancellationToken cancellationToken = default)
    {
        var operation = new OperationRequest(query, variables: variables);
        return uri is null
            ? PostAsync(client, operation, cancellationToken)
            : PostAsync(client, operation, uri, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="operation">
    /// The GraphQL operation request.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        OperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        var request = new GraphQLHttpRequest(operation) { Method = GraphQLHttpMethod.Post, };
        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="batch">
    /// The GraphQL variable batch request.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        VariableBatchRequest batch,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        var request = new GraphQLHttpRequest(batch) { Method = GraphQLHttpMethod.Post, };
        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="batch">
    /// The GraphQL operation batch request.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        OperationBatchRequest batch,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        var request = new GraphQLHttpRequest(batch) { Method = GraphQLHttpMethod.Post, };
        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="operation">
    /// The GraphQL operation request.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
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

        var request = new GraphQLHttpRequest(operation, new Uri(uri))
        {
            Method = GraphQLHttpMethod.Post,
        };

        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="batch">
    /// The GraphQL variable batch request.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        VariableBatchRequest batch,
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

        var request = new GraphQLHttpRequest(batch, new Uri(uri))
        {
            Method = GraphQLHttpMethod.Post,
        };

        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="batch">
    /// The GraphQL operation batch request.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        OperationBatchRequest batch,
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

        var request = new GraphQLHttpRequest(batch, new Uri(uri))
        {
            Method = GraphQLHttpMethod.Post,
        };

        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="operation">
    /// The GraphQL operation request.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
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

        var request = new GraphQLHttpRequest(operation, uri)
        {
            Method = GraphQLHttpMethod.Post,
        };

        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="batch">
    /// The GraphQL variable batch request.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        VariableBatchRequest batch,
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

        var request = new GraphQLHttpRequest(batch, uri)
        {
            Method = GraphQLHttpMethod.Post,
        };

        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a GraphQL POST request to the specified GraphQL endpoint.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> to send the request with.
    /// </param>
    /// <param name="batch">
    /// The GraphQL operation batch request.
    /// </param>
    /// <param name="uri">
    /// The GraphQL request URI.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// </returns>
    public static Task<GraphQLHttpResponse> PostAsync(
        this GraphQLHttpClient client,
        OperationBatchRequest batch,
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

        var request = new GraphQLHttpRequest(batch, uri)
        {
            Method = GraphQLHttpMethod.Post,
        };

        return client.SendAsync(request, cancellationToken);
    }
}
