namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OperationsFileOption : Option<string>
{
    public OperationsFileOption() : base("--operations-file")
    {
        Description = "The path to the json file with the operations";
        IsRequired = true;
        this.DefaultFileFromEnvironmentValue("OPERATIONS_FILE");
        this.LegalFilePathsOnly();
    }
}
