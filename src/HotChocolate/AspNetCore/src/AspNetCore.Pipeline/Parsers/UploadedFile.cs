using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Parsers;

internal sealed class UploadedFile(IFormFile file) : IFile
{
    public string Name => file.FileName;

    public long? Length => file.Length;

    public string? ContentType => file.ContentType;

    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        => file.CopyToAsync(target, cancellationToken);

    public Stream OpenReadStream()
        => file.OpenReadStream();
}
