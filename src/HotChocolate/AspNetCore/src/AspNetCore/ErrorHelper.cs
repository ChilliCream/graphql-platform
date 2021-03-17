using HotChocolate.AspNetCore.Properties;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore
{
    /// <summary>
    /// An internal helper class that centralizes server errors.
    /// </summary>
    internal static class ErrorHelper
    {
        public static IError InvalidRequest() =>
            ErrorBuilder.New()
                .SetMessage(AspNetCoreResources.ErrorHelper_InvalidRequest)
                .SetCode(ErrorCodes.Server.RequestInvalid)
                .Build();

        public static IError RequestHasNoElements() =>
            ErrorBuilder.New()
                .SetMessage(AspNetCoreResources.ErrorHelper_RequestHasNoElements)
                .SetCode(ErrorCodes.Server.RequestInvalid)
                .Build();

        public static IQueryResult ResponseTypeNotSupported() =>
            QueryResultBuilder.CreateError(
                ErrorBuilder.New()
                    .SetMessage(AspNetCoreResources.ErrorHelper_ResponseTypeNotSupported)
                    .Build());
    }
}
