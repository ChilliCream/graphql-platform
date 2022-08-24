using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

internal sealed class UploadedFile : StreamFile
{
    private readonly IFormFile _file;

    public UploadedFile(IFormFile file)
        : base(file.FileName, file.OpenReadStream, file.Length, file.ContentType)
    {
        _file = file ?? throw new ArgumentNullException(nameof(file));
    }

    public override Task CopyToAsync(
        Stream target,
        CancellationToken cancellationToken = default) =>
        _file.CopyToAsync(target, cancellationToken);
}
