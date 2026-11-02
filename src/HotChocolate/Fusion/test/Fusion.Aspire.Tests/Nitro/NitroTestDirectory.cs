namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// A temporary directory that stands in for the Nitro configuration root so no test ever reads
/// or writes the real home directory of the machine.
/// </summary>
internal sealed class NitroTestDirectory : IDisposable
{
    public NitroTestDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "hc-nitro-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// Gets the full path of the directory.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the full path of a file inside the directory.
    /// </summary>
    public string GetPath(string fileName) => System.IO.Path.Combine(Path, fileName);

    /// <summary>
    /// Writes a file into the directory and returns its full path.
    /// </summary>
    public string WriteFile(string fileName, string content)
    {
        var path = GetPath(fileName);
        File.WriteAllText(path, content);

        return path;
    }

    /// <summary>
    /// Writes a file into the directory and returns its full path.
    /// </summary>
    public string WriteFile(string fileName, byte[] content)
    {
        var path = GetPath(fileName);
        File.WriteAllBytes(path, content);

        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A directory that cannot be deleted is left for the operating system to clean up.
        }
    }
}
