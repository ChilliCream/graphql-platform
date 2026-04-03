namespace ChilliCream.Nitro.CommandLine;

internal sealed class OperationsFileOption : Option<string>
{
    public OperationsFileOption() : base("--operations-file")
    {
        Description = "The path to the json file with the operations";
        Required = true;
        this.DefaultFileFromEnvironmentValue("OPERATIONS_FILE");
        this.LegalFilePathsOnly();
    }
}
