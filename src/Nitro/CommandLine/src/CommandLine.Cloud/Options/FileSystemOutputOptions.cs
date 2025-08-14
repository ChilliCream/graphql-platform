namespace ChilliCream.Nitro.CLI.Option;

internal sealed class FileSystemOutputOptions : Option<FileSystemInfo>
{
    public FileSystemOutputOptions() : base("--output")
    {
        Description = "The path where the client is stored";
        IsRequired = true;
    }
}
