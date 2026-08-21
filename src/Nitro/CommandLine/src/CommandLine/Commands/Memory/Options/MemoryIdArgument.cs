using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryIdArgument : Argument<string>
{
    public MemoryIdArgument() : base("id")
    {
        Description = "The memory ID";

        // Rejects anything that is not a well-formed id up front, so a
        // value like "../../x" can never reach a Path.Combine call that
        // builds a curated file path from it.
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
