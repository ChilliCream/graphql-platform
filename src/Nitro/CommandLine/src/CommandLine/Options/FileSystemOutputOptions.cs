namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class FileSystemOutputOptions : Option<string>
{
    public FileSystemOutputOptions() : base("--output")
    {
        Description = "The path where the client is stored";
        IsRequired = true;
        this.LegalFilePathsOnly();
    }
}
