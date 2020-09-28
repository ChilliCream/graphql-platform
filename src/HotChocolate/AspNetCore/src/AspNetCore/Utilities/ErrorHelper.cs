using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Utilities
{
    internal static class ErrorHelper
    {
        public static IError InvalidRequest() =>
            ErrorBuilder.New()
                .SetMessage("Invalid GraphQL Request.")
                .SetCode(ErrorCodes.Server.RequestInvalid)
                .Build();

        public static IError RequestHasNoElements() =>
            ErrorBuilder.New()
                .SetMessage("The GraphQL batch request has no elements.")
                .SetCode(ErrorCodes.Server.RequestInvalid)
                .Build();

        public static IQueryResult ResponseTypeNotSupported() =>
            QueryResultBuilder.CreateError(
                ErrorBuilder.New()
                    .SetMessage("The response type is not supported.")
                    .Build());
    }
}
