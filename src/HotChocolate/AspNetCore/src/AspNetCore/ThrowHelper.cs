using System;
using HotChocolate.Language;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;

namespace HotChocolate.AspNetCore
{
    internal static class ThrowHelper
    {
        public static GraphQLRequestException DefaultHttpRequestParser_QueryAndIdMissing() =>
            new(ErrorBuilder.New()
                .SetMessage(ThrowHelper_DefaultHttpRequestParser_QueryAndIdMissing)
                .SetCode(ErrorCodes.Server.QueryAndIdMissing)
                .Build());

        public static GraphQLRequestException DefaultHttpRequestParser_SyntaxError(
            SyntaxException ex) =>
            new(ErrorBuilder.New()
                .SetMessage(ex.Message)
                .AddLocation(ex.Line, ex.Column)
                .SetCode(ErrorCodes.Server.SyntaxError)
                .Build());

        public static GraphQLRequestException DefaultHttpRequestParser_UnexpectedError(
            Exception ex) =>
            new(ErrorBuilder.New()
                .SetMessage(ex.Message)
                .SetException(ex)
                .SetCode(ErrorCodes.Server.UnexpectedRequestParserError)
                .Build());

        public static GraphQLRequestException DefaultHttpRequestParser_RequestIsEmpty() =>
            new(ErrorBuilder.New()
                .SetMessage(ThrowHelper_DefaultHttpRequestParser_RequestIsEmpty)
                .SetCode(ErrorCodes.Server.RequestInvalid)
                .Build());

        public static GraphQLRequestException DefaultHttpRequestParser_MaxRequestSizeExceeded() =>
            new(ErrorBuilder.New()
                .SetMessage(ThrowHelper_DefaultHttpRequestParser_MaxRequestSizeExceeded)
                .SetCode(ErrorCodes.Server.MaxRequestSize)
                .Build());

        public static NotSupportedException  DataStartMessageHandler_RequestTypeNotSupported() =>
            new(ThrowHelper_DataStartMessageHandler_RequestTypeNotSupported);
    }
}
