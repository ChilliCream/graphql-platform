using ChilliCream.Nitro.CLI.Exceptions;

namespace ChilliCream.Nitro.CLI.Helpers;

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
