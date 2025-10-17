namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class OperationsFileOption : Option<FileInfo>
{
    public OperationsFileOption() : base("--operations-file")
    {
        Description = "The path to the json file with the operations";
        IsRequired = true;
        this.DefaultFileFromEnvironmentValue("OPERATIONS_FILE");
    }
}
