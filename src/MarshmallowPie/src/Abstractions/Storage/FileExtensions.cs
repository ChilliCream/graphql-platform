using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Storage
{
    public static class FileExtensions
    {
        public static Task CreateTextFileAsync(
            this IFileContainer container,
            Guid fileName,
            string text,
            CancellationToken cancellationToken = default) =>
            CreateTextFileAsync(
                container,
                fileName.ToString("N", CultureInfo.InvariantCulture),
                text,
                cancellationToken);

        public static Task CreateTextFileAsync(
            this IFileContainer container,
            string fileName,
            string text,
            CancellationToken cancellationToken = default)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            return container.CreateFileAsync(fileName, buffer, 0, buffer.Length, cancellationToken);
        }

        public static async Task<string> ReadAllTextAsync(
            this IFileContainer container,
            Guid fileName,
            CancellationToken cancellationToken = default)
        {
            IFile file =
                await container.GetFileAsync(
                    fileName.ToString("N", CultureInfo.InvariantCulture),
                    cancellationToken)
                    .ConfigureAwait(false);

            return await file.ReadAllTextAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task<string> ReadAllTextAsync(
            this IFile file,
            CancellationToken cancellationToken)
        {
            using var fs = await file.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var sr = new StreamReader(fs);
            return await sr.ReadToEndAsync().ConfigureAwait(false);
        }
    }
}
