namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalOutputFileOption : Option<string>
{
    public const string OptionName = "--output-file";

    public OptionalOutputFileOption() : base(OptionName)
    {
        Description = "The file path to write the output to";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.OutputFile);
        this.LegalFilePathsOnly();
    }
}
