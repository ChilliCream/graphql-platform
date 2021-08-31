using HotChocolate.Types;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Utilities
{
    public class UploadQuery
    {
        public async Task<string> SingleUpload(IFile file)
        {
            await using Stream stream = file.OpenReadStream();
            using var sr = new StreamReader(stream, Encoding.UTF8);
            return await sr.ReadToEndAsync();
        }

        public async Task<string> ObjectUpload(
            InputWithFile input)
        {
            await using Stream stream = input.File.OpenReadStream();
            using var sr = new StreamReader(stream, Encoding.UTF8);
            return await sr.ReadToEndAsync();
        }

        public async Task<string> ListUpload(
            InputWithFile[][] input)
        {
            await using Stream stream = input[0][0].File.OpenReadStream();
            using var sr = new StreamReader(stream, Encoding.UTF8);
            return await sr.ReadToEndAsync();
        }

        public async Task<string> OptionalUpload(
            [GraphQLType(typeof(UploadType))]
            Optional<IFile> file)
        {
            await using Stream stream = file.Value.OpenReadStream();
            using var sr = new StreamReader(stream, Encoding.UTF8);
            return await sr.ReadToEndAsync();
        }

        public async Task<string> OptionalObjectUpload(
            InputWithOptionalFile input)
        {
            await using Stream stream = input.File.Value.OpenReadStream();
            using var sr = new StreamReader(stream, Encoding.UTF8);
            return await sr.ReadToEndAsync();
        }
    }

    public class InputWithOptionalFile
    {
        [GraphQLType(typeof(UploadType))]
        public Optional<IFile> File { get; set; }
    }

    public class InputWithFile
    {
        [GraphQLType(typeof(NonNullType<UploadType>))]
        public IFile File { get; set; }
    }
}
