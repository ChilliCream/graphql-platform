using HotChocolate.OpenApi.Models;

namespace HotChocolate.OpenApi.Helpers;

internal sealed class OperationHttpRequestHelper
{
    public static HttpRequestMessage CreateRequest(Operation operation)
    {
        var request = new HttpRequestMessage(operation.Method, operation.Path);
        return request;
    }
}
