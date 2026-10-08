using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using ThrowHelper = HotChocolate.AspNetCore.Utilities.ThrowHelper;

namespace HotChocolate.AspNetCore.Parsers;

/// <summary>
/// Reads the form of a <c>multipart/form-data</c> request, applying
/// <see cref="FormOptions.MultipartBodyLengthLimit"/> to every section and the maximum
/// request size to the <c>operations</c> field as well.
/// </summary>
internal static class MultipartFormReader
{
    private const string MultipartFormData = "multipart/form-data";
    private const string Operations = "operations";
    private static readonly Func<string> s_tempDirectoryAccessor = GetTempDirectory;

    /// <summary>
    /// Reads the form of <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The request whose body is read.</param>
    /// <param name="options">The limits and the buffering of the form.</param>
    /// <param name="maxRequestSize">The maximum size, in bytes, of <c>operations</c>.</param>
    /// <param name="cancellationToken">The token that cancels the read.</param>
    public static async Task<IFormCollection> ReadAsync(
        HttpRequest request,
        FormOptions options,
        int maxRequestSize,
        CancellationToken cancellationToken)
    {
        if (request.HttpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false })
        {
            throw ThrowHelper.MultipartFormReader_AntiforgeryValidationFailed();
        }

        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType)
            || !contentType.MediaType.Equals(MultipartFormData, StringComparison.OrdinalIgnoreCase))
        {
            throw ThrowHelper.MultipartFormReader_IncorrectContentType(request.ContentType);
        }

        if (request.ContentLength == 0)
        {
            return FormCollection.Empty;
        }

        if (options.BufferBody)
        {
            request.Body = new LengthLimitedReadStream(
                request.Body,
                options.BufferBodyLengthLimit,
                static () => ThrowHelper.RequestBodyTooLarge());
            request.EnableBuffering(options.MemoryBufferThreshold);
        }

        var reader = new MultipartReader(
            GetBoundary(contentType, options.MultipartBoundaryLengthLimit),
            request.Body)
        {
            HeadersCountLimit = options.MultipartHeadersCountLimit,
            HeadersLengthLimit = options.MultipartHeadersLengthLimit,
            BodyLengthLimit = null
        };
        var fields = new KeyValueAccumulator();
        FormFileCollection? files = null;
        var sectionCount = 0;
        var section = await reader.ReadNextSectionAsync(cancellationToken);

        while (section is not null)
        {
            sectionCount++;

            if (sectionCount > options.ValueCountLimit)
            {
                throw ThrowHelper.MultipartFormReader_ValueCountLimitExceeded(
                    options.ValueCountLimit);
            }

            if (!ContentDispositionHeaderValue.TryParse(
                section.ContentDisposition,
                out var contentDisposition))
            {
                throw ThrowHelper.MultipartFormReader_InvalidContentDisposition(
                    section.ContentDisposition);
            }

            if (contentDisposition.IsFileDisposition())
            {
                var file = await ReadFileAsync(
                    section,
                    contentDisposition,
                    options,
                    request,
                    cancellationToken);
                files ??= [];
                files.Add(file);
            }
            else if (contentDisposition.IsFormDisposition())
            {
                var formSection = new FormMultipartSection(section, contentDisposition);
                var name = formSection.Name;
                var limitToMaxRequestSize = name == Operations
                    && maxRequestSize <= options.MultipartBodyLengthLimit;
                section.Body = limitToMaxRequestSize
                    ? new LengthLimitedReadStream(
                        section.Body,
                        maxRequestSize,
                        static () => ThrowHelper.DefaultHttpRequestParser_MaxRequestSizeExceeded())
                    : new LengthLimitedReadStream(
                        section.Body,
                        options.MultipartBodyLengthLimit,
                        () => ThrowHelper.MultipartFormReader_SectionTooLarge(name));
                fields.Append(name, await formSection.GetValueAsync(cancellationToken));
            }
            else
            {
                var name = HeaderUtilities.RemoveQuotes(contentDisposition.Name).ToString();
                var skippedBody = new LengthLimitedReadStream(
                    section.Body,
                    options.MultipartBodyLengthLimit,
                    () => ThrowHelper.MultipartFormReader_SectionTooLarge(name));
                await skippedBody.DrainAsync(cancellationToken);
            }

            section = await reader.ReadNextSectionAsync(cancellationToken);
        }

        if (request.Body.CanSeek)
        {
            request.Body.Seek(0, SeekOrigin.Begin);
        }

        return new FormCollection(fields.GetResults(), files);
    }

    private static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
    {
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary);

        if (StringSegment.IsNullOrEmpty(boundary))
        {
            throw ThrowHelper.MultipartFormReader_MissingBoundary();
        }

        if (boundary.Length > lengthLimit)
        {
            throw ThrowHelper.MultipartFormReader_BoundaryLengthLimitExceeded(lengthLimit);
        }

        return boundary.ToString();
    }

    private static string GetTempDirectory()
    {
        var directory = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP")
            ?? System.IO.Path.GetTempPath();

        if (!Directory.Exists(directory))
        {
            throw ThrowHelper.MultipartFormReader_TempDirectoryNotFound(directory);
        }

        return directory;
    }

    private static async Task<FormFile> ReadFileAsync(
        MultipartSection section,
        ContentDispositionHeaderValue contentDisposition,
        FormOptions options,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var fileSection = new FileMultipartSection(section, contentDisposition);
        var name = fileSection.Name;
        var limitedBody = new LengthLimitedReadStream(
            section.Body,
            options.MultipartBodyLengthLimit,
            () => ThrowHelper.MultipartFormReader_SectionTooLarge(name));
        FormFile file;

        if (section.BaseStreamOffset is { } offset)
        {
            await limitedBody.DrainAsync(cancellationToken);
            file = new FormFile(
                request.Body,
                offset,
                section.Body.Length,
                name,
                fileSection.FileName);
        }
        else
        {
            var bufferedBody = new FileBufferingReadStream(
                limitedBody,
                options.MemoryBufferThreshold,
                bufferLimit: null,
                s_tempDirectoryAccessor);
            request.HttpContext.Response.RegisterForDispose(bufferedBody);
            await bufferedBody.DrainAsync(cancellationToken);
            file = new FormFile(
                bufferedBody,
                0,
                bufferedBody.Length,
                name,
                fileSection.FileName);
        }

        file.Headers = new HeaderDictionary(section.Headers);

        return file;
    }
}
