namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryMaxCharsOption : Option<int>
{
    public MemoryMaxCharsOption() : base("--max-chars")
    {
        Description = "The character budget for the assembled prompt-ready text";
        Required = false;
        DefaultValueFactory = _ => 20000;

        Validators.Add(result =>
        {
            var maxChars = result.GetValue(this);

            if (maxChars <= 0)
            {
                result.AddError("Option '--max-chars' must be a positive number.");
            }
        });
    }
}
