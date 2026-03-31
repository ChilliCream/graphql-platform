namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalOutputFileOption : Option<string>
{
    public OptionalOutputFileOption() : base("--output-file")
    {
        Description = "The file path to write the output to";
        Required = false;
        this.DefaultFromEnvironmentValue("OUTPUT_FILE");
        this.LegalFilePathsOnly();
    }
}
