using HotChocolate.Language;
using static HotChocolate.AspNetCore.Properties.AspNetCorePipelineResources;

namespace HotChocolate.AspNetCore.Utilities;

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
            .AddLocation(new Location(ex.Line, ex.Column))
            .SetCode(ErrorCodes.Server.SyntaxError)
            .Build());

    public static GraphQLRequestException DefaultHttpRequestParser_UnexpectedError(
        Exception ex) =>
        new(
            ErrorBuilder.New()
                .SetMessage(ex.Message)
                .SetException(ex)
                .SetCode(ErrorCodes.Server.UnexpectedRequestParserError)
                .Build(),
            ex);

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

    public static GraphQLRequestException RequestBodyTooLarge() =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_RequestBodyTooLarge)
            .SetCode(ErrorCodes.Server.RequestBodyTooLarge)
            .Build());

    public static GraphQLRequestException HttpMultipartMiddleware_Invalid_Form(Exception ex) =>
        new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_HttpMultipartMiddleware_Invalid_Form)
                .SetException(ex)
                .SetCode(ErrorCodes.Server.MultiPartInvalidForm)
                .SetExtension("underlyingError", ex.Message)
                .Build());

    public static GraphQLRequestException HttpMultipartMiddleware_No_Operations_Specified() =>
        new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_HttpMultipartMiddleware_No_Operations_Specified)
                .SetCode(ErrorCodes.Server.MultiPartNoOperationsSpecified)
                .Build());

    public static GraphQLRequestException HttpMultipartMiddleware_Fields_Misordered() =>
        new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_HttpMultipartMiddleware_Fields_Misordered)
                .SetCode(ErrorCodes.Server.MultiPartFieldsMisordered)
                .Build());

    public static GraphQLRequestException HttpMultipartMiddleware_NoObjectPath(string filename) =>
        new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_HttpMultipartMiddleware_NoObjectPath, filename)
                .SetCode(ErrorCodes.Server.MultiPartNoObjectPath)
                .Build());

    public static GraphQLRequestException HttpMultipartMiddleware_FileMissing(string filename) =>
        new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_HttpMultipartMiddleware_FileMissing, filename)
                .SetCode(ErrorCodes.Server.MultiPartFileMissing)
                .Build());

    public static GraphQLRequestException HttpMultipartMiddleware_FileVariableValueNotNull(string fileKey) =>
        new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_HttpMultipartMiddleware_FileVariableValueNotNull, fileKey)
                .SetCode(ErrorCodes.Server.MultiPartFileVariableValueNotNull)
                .Build());

    public static GraphQLRequestException HttpMultipartMiddleware_VariableStructureInvalid() =>
        new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_HttpMultipartMiddleware_VariableStructureInvalid)
                .SetCode(ErrorCodes.Server.MultiPartVariableStructureInvalid)
                .Build());

    public static GraphQLRequestException HttpMultipartMiddleware_InvalidPath(string path) =>
        new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_HttpMultipartMiddleware_InvalidPath, path)
                .SetCode(ErrorCodes.Server.MultiPartInvalidPath)
                .Build());

    public static GraphQLRequestException HttpMultipartMiddleware_PathMustStartWithVariable() =>
        new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_HttpMultipartMiddleware_PathMustStartWithVariable)
                .SetCode(ErrorCodes.Server.MultiPartPathMustStartWithVariable)
                .Build());

    public static GraphQLRequestException HttpMultipartMiddleware_InvalidMapJson() =>
        new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_HttpMultipartMiddleware_InvalidMapJson)
                .SetCode(ErrorCodes.Server.MultiPartInvalidMapJson)
                .Build());

    public static GraphQLRequestException HttpMultipartMiddleware_MapNotSpecified() =>
        new GraphQLRequestException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_HttpMultipartMiddleware_MapNotSpecified)
                .SetCode(ErrorCodes.Server.MultiPartMapNotSpecified)
                .Build());

    public static InvalidOperationException MultipartFormReader_AntiforgeryValidationFailed() =>
        new(ThrowHelper_MultipartFormReader_AntiforgeryValidationFailed);

    public static InvalidDataException MultipartFormReader_BoundaryLengthLimitExceeded(int limit) =>
        new(string.Format(ThrowHelper_MultipartFormReader_BoundaryLengthLimitExceeded, limit));

    public static InvalidOperationException MultipartFormReader_IncorrectContentType(
        string? contentType) =>
        new(string.Format(ThrowHelper_MultipartFormReader_IncorrectContentType, contentType));

    public static InvalidDataException MultipartFormReader_InvalidContentDisposition(
        string? contentDisposition) =>
        new(string.Format(
            ThrowHelper_MultipartFormReader_InvalidContentDisposition,
            contentDisposition));

    public static InvalidDataException MultipartFormReader_MissingBoundary() =>
        new(ThrowHelper_MultipartFormReader_MissingBoundary);

    public static GraphQLRequestException MultipartFormReader_SectionTooLarge(string name) =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_MultipartFormReader_SectionTooLarge, name)
            .SetCode(ErrorCodes.Server.MultiPartSectionTooLarge)
            .Build());

    public static DirectoryNotFoundException MultipartFormReader_TempDirectoryNotFound(
        string directory) =>
        new(string.Format(ThrowHelper_MultipartFormReader_TempDirectoryNotFound, directory));

    public static InvalidDataException MultipartFormReader_ValueCountLimitExceeded(int limit) =>
        new(string.Format(ThrowHelper_MultipartFormReader_ValueCountLimitExceeded, limit));

    public static NotSupportedException Formatter_ResultKindNotSupported()
        => new(ThrowHelper_Formatter_ResultKindNotSupported);

    public static NotSupportedException Formatter_ResponseContentTypeNotSupported(
        string contentType)
        => new(string.Format(ThrowHelper_Formatter_ResponseContentTypeNotSupported, contentType));

    public static ArgumentOutOfRangeException Formatter_TransportVersionNotSupported(
        string paramName,
        HttpTransportVersion transportVersion)
        => new(
            paramName,
            transportVersion,
            string.Format(ThrowHelper_Formatter_TransportVersionNotSupported, transportVersion));
}
