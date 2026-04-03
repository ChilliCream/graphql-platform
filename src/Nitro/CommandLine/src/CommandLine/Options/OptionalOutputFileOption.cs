namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalOutputFileOption : Option<string>
{
    public OptionalOutputFileOption() : base("--output-file")
    {
        Description = "The file path to write the output to";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.OutputFile);
        this.LegalFilePathsOnly();
    }
}
