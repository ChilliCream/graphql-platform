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

        public static GraphQLException HttpMultipartMiddleware_Form_Incomplete() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_HttpMultipartMiddleware_Form_Incomplete)
                    .SetCode(ErrorCodes.Server.MultiPartFormIncomplete)
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
                    .SetMessage("No object paths specified for key '{0}' in 'map'.", filename)
                    .SetCode("// TODO CODE HC")
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_FileMissing(string filename) =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage("File of key '{0}' is missing.", filename)
                    .SetCode("// TODO CODE HC")
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_VariableNotFound(string path) =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage("The variable path '{0}' is invalid.", path)
                    .SetCode("// TODO CODE HC")
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_VariableStructureInvalid() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage("The variable structure is invalid.")
                    .SetCode("// TODO CODE HC")
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_InvalidPath(string path) =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage("Invalid variable path `{0}` in `map`.", path)
                    .SetCode("// TODO CODE HC")
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_PathMustStartWithVariable() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage("The variable path must start with `variables`.")
                    .SetCode("// TODO CODE HC")
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_InvalidMapJson() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage("Invalid JSON in the `map` multipart field; Expected type of Dictionary<string, string[]>.")
                    .SetCode("// TODO CODE HC")
                    .Build());

        public static GraphQLException HttpMultipartMiddleware_MapNotSpecified() =>
            new GraphQLRequestException(
                ErrorBuilder.New()
                    .SetMessage("No `map` specified.")
                    .SetCode("// TODO CODE HC")
                    .Build());

    }
}
