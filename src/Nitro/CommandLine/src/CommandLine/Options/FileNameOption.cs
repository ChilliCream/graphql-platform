namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class FileNameOption : Option<FileInfo>
{
    public FileNameOption() : base("--file")
    {
        Description = "The file where the schema is stored";
        IsRequired = true;
    }
}
