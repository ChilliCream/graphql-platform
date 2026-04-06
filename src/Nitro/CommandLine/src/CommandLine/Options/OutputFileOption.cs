namespace ChilliCream.Nitro.CommandLine;

internal class OutputFileOption : Option<string>
{
    public const string OptionName = "--output-file";

    public OutputFileOption() : base(OptionName)
    {
        Description = "The file path to write the output to";
        Required = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.OutputFile);
        this.LegalFilePathsOnly();
    }
}
