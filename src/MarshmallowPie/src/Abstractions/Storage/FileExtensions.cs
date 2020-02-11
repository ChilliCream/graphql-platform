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
    }
}
