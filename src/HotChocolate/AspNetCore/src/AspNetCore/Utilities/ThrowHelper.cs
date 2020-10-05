using System;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Utilities
{
    internal static class ThrowHelper
    {
        public static GraphQLRequestException DefaultHttpRequestParser_QueryAndIdMissing() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage("Either the parameter query or id has to be set.")
                    .Build());

        public static GraphQLRequestException DefaultHttpRequestParser_SyntaxError(
            SyntaxException ex) =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetCode(ErrorCodes.Execution.SyntaxError)
                    .AddLocation(ex.Line, ex.Column)
                    .Build());

        public static GraphQLRequestException DefaultHttpRequestParser_UnexpectedError(
            Exception ex) =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetException(ex)
                    .Build());

        public static GraphQLRequestException DefaultHttpRequestParser_RequestIsEmpty() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage("The GraphQL request is empty.")
                    .SetCode(ErrorCodes.Server.RequestInvalid)
                    .Build());

        public static GraphQLRequestException DefaultHttpRequestParser_MaxRequestSizeExceeded() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage("Max GraphQL request size reached.")
                    .SetCode(ErrorCodes.Server.MaxRequestSize)
                    .Build());

        public static NotSupportedException  DataStartMessageHandler_RequestTypeNotSupported() =>
            new NotSupportedException("The response type is not supported.");
    }
}
