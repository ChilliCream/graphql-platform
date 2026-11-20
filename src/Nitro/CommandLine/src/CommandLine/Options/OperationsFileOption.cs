namespace ChilliCream.Nitro.CommandLine;

internal sealed class OperationsFileOption : Option<string>
{
    public OperationsFileOption() : base("--operations-file")
    {
        Description = "The path to the JSON file with the operations (Relay-style or Apollo persisted query manifest)";
        Required = true;
        this.DefaultFileFromEnvironmentValue(EnvironmentVariables.OperationsFile);
        this.LegalFilePathsOnly();
    }
}
