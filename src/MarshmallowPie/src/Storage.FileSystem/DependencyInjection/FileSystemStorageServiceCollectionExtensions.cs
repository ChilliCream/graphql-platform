using System.IO;
using MarshmallowPie;
using MarshmallowPie.Storage;
using MarshmallowPie.Storage.FileSystem;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FileSystemStorageServiceCollectionExtensions
    {
        public static IServiceCollection AddFileSystemStorage(
            this IServiceCollection serviceCollection,
            string rootDirectoryPath)
        {
            if(!Directory.Exists(rootDirectoryPath))
            {
                throw new DirectoryNotFoundException(
                    $"The directory `{Path.GetFullPath(rootDirectoryPath)}` must exist in order to " +
                    "use it as file storage.");
            }

            return serviceCollection.AddSingleton<IFileStorage>(sp => new FileStorage(rootDirectoryPath));
        }
    }
}
