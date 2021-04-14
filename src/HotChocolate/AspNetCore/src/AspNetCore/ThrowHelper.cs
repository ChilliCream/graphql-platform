using System;
using HotChocolate.Language;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;

namespace HotChocolate.AspNetCore
{
    /// <summary>
    /// An internal helper class that centralizes the server exceptions.
    /// </summary>
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

        public static NotSupportedException DataStartMessageHandler_RequestTypeNotSupported() =>
            new(ThrowHelper_DataStartMessageHandler_RequestTypeNotSupported);

        public static GraphQLException HttpMultipartMiddleware_Invalid_Form(
            Exception ex) =>
             new GraphQLRequestException(
                 ErrorBuilder.New()
                    .SetMessage(ThrowHelper_HttpMultipartMiddleware_Invalid_Form)
                    .SetException(ex)
                    .SetCode(ErrorCodes.Server.MultiPartInvalidForm)
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_No_Operations_Specified() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_HttpMultipartMiddleware_No_Operations_Specified)
                    .SetCode(ErrorCodes.Server.MultiPartNoOperationsSpecified)
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_Fields_Misordered() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_HttpMultipartMiddleware_Fields_Misordered)
                    .SetCode(ErrorCodes.Server.MultiPartFieldsMisordered)
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_NoObjectPath(string filename) =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_HttpMultipartMiddleware_NoObjectPath, filename)
                    .SetCode(ErrorCodes.Server.MultiPartNoObjectPath)
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_FileMissing(string filename) =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_HttpMultipartMiddleware_FileMissing, filename)
                    .SetCode(ErrorCodes.Server.MultiPartFileMissing)
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_VariableNotFound(string path) =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_HttpMultipartMiddleware_VariableNotFound, path)
                    .SetCode(ErrorCodes.Server.MultiPartVariableNotFound)
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_VariableStructureInvalid() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_HttpMultipartMiddleware_VariableStructureInvalid)
                    .SetCode(ErrorCodes.Server.MultiPartVariableStructureInvalid)
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_InvalidPath(string path) =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_HttpMultipartMiddleware_InvalidPath, path)
                    .SetCode(ErrorCodes.Server.MultiPartInvalidPath)
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_PathMustStartWithVariable() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_HttpMultipartMiddleware_PathMustStartWithVariable)
                    .SetCode(ErrorCodes.Server.MultiPartPathMustStartWithVariable)
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_InvalidMapJson() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_HttpMultipartMiddleware_InvalidMapJson)
                    .SetCode(ErrorCodes.Server.MultiPartInvalidMapJson)
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_MapNotSpecified() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_HttpMultipartMiddleware_MapNotSpecified)
                    .SetCode(ErrorCodes.Server.MultiPartMapNotSpecified)
                    .Build());
    }
}
