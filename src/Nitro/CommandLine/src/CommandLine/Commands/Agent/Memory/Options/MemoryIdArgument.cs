using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;

internal sealed class MemoryIdArgument : Argument<string>
{
    public MemoryIdArgument() : base("id")
    {
        Description = "The memory ID";

        // Rejects anything that is not a well-formed id.
        Validators.Add(result =>
        {
            var id = result.GetValue(this);

            if (id is not null && !MemoryId.IsValid(id))
            {
                result.AddError($"'{id}' is not a valid memory ID.");
            }
        });
    }
}
