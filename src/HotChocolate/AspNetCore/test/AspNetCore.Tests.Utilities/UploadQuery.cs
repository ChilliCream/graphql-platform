using System.Text;
using HotChocolate.Types;

namespace HotChocolate.AspNetCore.Tests.Utilities;

public class UploadQuery
{
    public async Task<string> SingleUpload(IFile file)
    {
        await using var stream = file.OpenReadStream();
        using var sr = new StreamReader(stream, Encoding.UTF8);
        return await sr.ReadToEndAsync();
    }

    public async Task<string> ObjectUpload(
        InputWithFile input)
    {
        await using var stream = input.File.OpenReadStream();
        using var sr = new StreamReader(stream, Encoding.UTF8);
        return await sr.ReadToEndAsync();
    }

    public async Task<string> ListUpload(
        InputWithFile[][] input)
    {
        await using var stream = input[0][0].File.OpenReadStream();
        using var sr = new StreamReader(stream, Encoding.UTF8);
        return await sr.ReadToEndAsync();
    }

    public async Task<string> OptionalUpload([GraphQLType(typeof(UploadType))] Optional<IFile> file)
    {
        await using var stream = file.Value!.OpenReadStream();
        using var sr = new StreamReader(stream, Encoding.UTF8);
        return await sr.ReadToEndAsync();
    }

    public async Task<string> OptionalObjectUpload(InputWithOptionalFile input)
    {
        await using var stream = input.File.Value!.OpenReadStream();
        using var sr = new StreamReader(stream, Encoding.UTF8);
        return await sr.ReadToEndAsync();
    }
}

public class InputWithOptionalFile
{
    [GraphQLType(typeof(UploadType))]
    public Optional<IFile> File { get; set; } = default!;
}

public class InputWithFile
{
    [GraphQLType<NonNullType<UploadType>>]
    public IFile File { get; set; } = null!;
}
