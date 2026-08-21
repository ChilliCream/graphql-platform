namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemorySearchQueryArgument : Argument<string>
{
    public MemorySearchQueryArgument() : base("query")
    {
        Description = "The search text, matched literally, never as FTS5 query syntax";

        Validators.Add(result =>
        {
            var value = result.GetValue(this);

            if (value?.Trim().Length == 0)
            {
                result.AddError("The search query must not be empty.");
            }
        });
    }
}
