using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    internal sealed class UploadedFile : IFile
    {
        private IFormFile _file;

        public UploadedFile(IFormFile file)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        public string Name => _file.FileName;

        public long Length => _file.Length;

        public Task CopyToAsync(
            Stream target, 
            CancellationToken cancellationToken = default) =>
            _file.CopyToAsync(target, cancellationToken);

        public Stream OpenReadStream() =>
            _file.OpenReadStream();
    }
}
