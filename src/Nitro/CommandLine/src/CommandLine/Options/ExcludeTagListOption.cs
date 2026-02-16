namespace ChilliCream.Nitro.CommandLine.Options;

public sealed class ExcludeTagListOption : Option<List<string>>
{
    public ExcludeTagListOption() : this(false)
    {
    }

    public ExcludeTagListOption(bool isRequired) : base("--exclude-by-tag")
    {
        Description = "One or more tags to exclude from the composition.";
        IsRequired = isRequired;
    }
}
