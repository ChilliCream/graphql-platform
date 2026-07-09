namespace ChilliCream.Nitro.CommandLine;

internal sealed class FileSystemOutputOptions : Option<string>
{
    public FileSystemOutputOptions() : base("--path")
    {
        Description = "The path where the client is stored";
        Required = true;
        this.LegalFilePathsOnly();
    }
}
