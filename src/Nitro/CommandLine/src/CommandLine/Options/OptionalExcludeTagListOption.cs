namespace ChilliCream.Nitro.CommandLine;

public sealed class OptionalExcludeTagListOption : Option<List<string>>
{
    public OptionalExcludeTagListOption() : base("--exclude-by-tag")
    {
        Description = "One or more tags to exclude from the composition";
        AllowMultipleArgumentsPerToken = true;
    }
}
