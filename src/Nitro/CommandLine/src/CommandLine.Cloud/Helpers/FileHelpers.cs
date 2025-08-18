namespace ChilliCream.Nitro.CommandLine.Cloud.Helpers;

internal static class FileHelpers
{
    public static Stream CreateFileStream(FileInfo fileInfo)
    {
        if (!fileInfo.Exists)
        {
            throw new ExitException($"[red] File {fileInfo} was not found![/]");
        }

        return fileInfo.OpenRead();
    }
}
