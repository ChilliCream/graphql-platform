using Microsoft.Extensions.DependencyInjection;
using MarshmallowPie.Storage;
using MarshmallowPie.Storage.FileSystem;
using System.IO;

namespace MarshmallowPie
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
