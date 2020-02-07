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
            CancellationToken cancellationToken) =>
            CreateTextFileAsync(
                container,
                fileName.ToString("N", CultureInfo.InvariantCulture),
                text,
                cancellationToken);

        public static async Task CreateTextFileAsync(
            this IFileContainer container,
            string fileName,
            string text,
            CancellationToken cancellationToken)
        {
            using Stream stream = await container.CreateFileAsync(
                fileName, cancellationToken)
                .ConfigureAwait(false);
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            await stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
        }
    }
}
