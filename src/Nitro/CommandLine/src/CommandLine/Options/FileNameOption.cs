namespace ChilliCream.Nitro.CommandLine;

internal sealed class FileNameOption : Option<string>
{
    public FileNameOption() : base("--file")
    {
        Description = "The file where the schema is stored";
        Required = true;
        this.LegalFilePathsOnly();
    }
}
