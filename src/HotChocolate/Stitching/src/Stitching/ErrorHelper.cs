using System.Net;

namespace HotChocolate.Stitching
{
    public static class ErrorHelper
    {
        public static IError HttpRequestClient_HttpError(
            HttpStatusCode statusCode,
            string? responseBody) =>
            ErrorBuilder.New()
                .SetMessage(
                    "HTTP error {0} while fetching from downstream service.",
                    statusCode)
                .SetCode(ErrorCodes.Stitching.HttpRequestException)
                .SetExtension("response", responseBody)
                .Build();
    }
}
