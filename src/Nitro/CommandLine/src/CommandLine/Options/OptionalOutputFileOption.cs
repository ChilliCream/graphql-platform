namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalOutputFileOption : Option<FileInfo>
{
    public OptionalOutputFileOption() : base("--output-file")
    {
        Description = "The output file";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("OUTPUT_FILE");
    }
}
