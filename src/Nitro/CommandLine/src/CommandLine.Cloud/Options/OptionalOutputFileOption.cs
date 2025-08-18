namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class OptionalOutputFileOption : Option<FileInfo>
{
    public OptionalOutputFileOption() : base("--output-file")
    {
        Description = "The output file";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("OUTPUT_FILE");
    }
}
