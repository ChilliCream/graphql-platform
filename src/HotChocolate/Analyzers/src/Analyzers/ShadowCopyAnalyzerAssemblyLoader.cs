using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static System.IO.Path;

namespace HotChocolate.Analyzers;

internal class ShadowCopyAnalyzerAssemblyLoader
{
    /// <summary>
    ///     The base directory for shadow copies. Each instance of
    ///     <see cref="ShadowCopyAnalyzerAssemblyLoader" /> gets its own
    ///     subdirectory under this directory. This is also the starting point
    ///     for scavenge operations.
    /// </summary>
    private readonly string _baseDirectory;

    /// <summary>
    ///     The directory where this instance of <see cref="ShadowCopyAnalyzerAssemblyLoader" />
    ///     will shadow-copy assemblies, and the mutex created to mark that the owner of it is still active.
    /// </summary>
    private readonly Lazy<(string directory, Mutex)> _shadowCopyDirectoryAndMutex;

    internal readonly Task DeleteLeftoverDirectoriesTask;

    /// <summary>
    ///     Used to generate unique names for per-assembly directories. Should be updated with
    ///     <see cref="Interlocked.Increment(ref int)" />.
    /// </summary>
    private int _assemblyDirectoryId;

    public ShadowCopyAnalyzerAssemblyLoader(string? baseDirectory = null)
    {
        if (baseDirectory != null)
            _baseDirectory = baseDirectory;
        else
            _baseDirectory =
                Combine(GetTempPath(), "HotChocolate", "AnalyzerShadowCopies");

        _shadowCopyDirectoryAndMutex = new Lazy<(string directory, Mutex)>(
            () => CreateUniqueDirectoryForProcess(), LazyThreadSafetyMode.ExecutionAndPublication);

        DeleteLeftoverDirectoriesTask = Task.Run(DeleteLeftoverDirectories);
    }

    private void DeleteLeftoverDirectories()
    {
        // Avoid first chance exception
        if (!Directory.Exists(_baseDirectory))
            return;

        IEnumerable<string> subDirectories;
        try
        {
            subDirectories = Directory.EnumerateDirectories(_baseDirectory);
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }

        foreach (var subDirectory in subDirectories)
        {
            var name = GetFileName(subDirectory)
                .ToLowerInvariant();
            Mutex? mutex = null;
            try
            {
                // We only want to try deleting the directory if no-one else is currently
                // using it. That is, if there is no corresponding mutex.
                if (!Mutex.TryOpenExisting(name, out mutex))
                {
                    ClearReadOnlyFlagOnFiles(subDirectory);
                    Directory.Delete(subDirectory, true);
                }
            }
            catch
            {
                // If something goes wrong we will leave it to the next run to clean up.
                // Just swallow the exception and move on.
            }
            finally
            {
                if (mutex != null)
                    mutex.Dispose();
            }
        }
    }

    internal string GetPathToLoad(string fullPath)
    {
        var assemblyDirectory = CreateUniqueDirectoryForAssembly();
        var shadowCopyPath = CopyFileAndResources(fullPath, assemblyDirectory);
        return shadowCopyPath;
    }

    private static string CopyFileAndResources(string fullPath, string assemblyDirectory)
    {
        var fileNameWithExtension = GetFileName(fullPath);
        var shadowCopyPath = Combine(assemblyDirectory, fileNameWithExtension);

        CopyFile(fullPath, shadowCopyPath);

        var originalDirectory = GetDirectoryName(fullPath)!;
        var fileNameWithoutExtension = GetFileNameWithoutExtension(fileNameWithExtension);
        var resourcesNameWithoutExtension = fileNameWithoutExtension + ".resources";
        var resourcesNameWithExtension = resourcesNameWithoutExtension + ".dll";

        foreach (var directory in Directory.EnumerateDirectories(originalDirectory))
        {
            var directoryName = GetFileName(directory);

            var resourcesPath = Combine(directory, resourcesNameWithExtension);
            if (File.Exists(resourcesPath))
            {
                var resourcesShadowCopyPath = Combine(assemblyDirectory, directoryName,
                    resourcesNameWithExtension);
                CopyFile(resourcesPath, resourcesShadowCopyPath);
            }

            resourcesPath = Combine(directory, resourcesNameWithoutExtension,
                resourcesNameWithExtension);
            if (File.Exists(resourcesPath))
            {
                var resourcesShadowCopyPath = Combine(assemblyDirectory, directoryName,
                    resourcesNameWithoutExtension, resourcesNameWithExtension);
                CopyFile(resourcesPath, resourcesShadowCopyPath);
            }
        }

        return shadowCopyPath;
    }

    private static void CopyFile(string originalPath, string shadowCopyPath)
    {
        var directory = GetDirectoryName(shadowCopyPath);
        Directory.CreateDirectory(directory);

        File.Copy(originalPath, shadowCopyPath);

        ClearReadOnlyFlagOnFile(new FileInfo(shadowCopyPath));
    }

    private static void ClearReadOnlyFlagOnFiles(string directoryPath)
    {
        var directory = new DirectoryInfo(directoryPath);

        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            ClearReadOnlyFlagOnFile(file);
        }
    }

    private static void ClearReadOnlyFlagOnFile(FileInfo fileInfo)
    {
        try
        {
            if (fileInfo.IsReadOnly)
                fileInfo.IsReadOnly = false;
        }
        catch
        {
            // There are many reasons this could fail. Ignore it and keep going.
        }
    }

    private string CreateUniqueDirectoryForAssembly()
    {
        var directoryId = Interlocked.Increment(ref _assemblyDirectoryId);

        var directory = Combine(_shadowCopyDirectoryAndMutex.Value.directory,
            directoryId.ToString());

        Directory.CreateDirectory(directory);
        return directory;
    }

    private (string directory, Mutex mutex) CreateUniqueDirectoryForProcess()
    {
        var guid = Guid.NewGuid()
            .ToString("N")
            .ToLowerInvariant();
        var directory = Combine(_baseDirectory, guid);

        var mutex = new Mutex(false, guid);

        Directory.CreateDirectory(directory);

        return (directory, mutex);
    }
}
