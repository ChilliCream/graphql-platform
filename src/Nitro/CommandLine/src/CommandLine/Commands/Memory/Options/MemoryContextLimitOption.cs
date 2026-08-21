namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryContextLimitOption : Option<int>
{
    public MemoryContextLimitOption() : base("--limit")
    {
        Description = "The maximum number of memories to admit";
        Required = false;
        DefaultValueFactory = _ => 50;

        Validators.Add(result =>
        {
            var limit = result.GetValue(this);

            if (limit <= 0)
            {
                result.AddError("Option '--limit' must be a positive number.");
            }
        });
    }
}
