namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;

internal sealed class MemoryLimitOption : Option<int?>
{
    public MemoryLimitOption() : base("--limit", "-n")
    {
        Description = "The maximum number of memories to show";
        Required = false;

        Validators.Add(result =>
        {
            var limit = result.GetValue(this);

            if (limit is <= 0)
            {
                result.AddError("Option '--limit' must be a positive number.");
            }
        });
    }
}
