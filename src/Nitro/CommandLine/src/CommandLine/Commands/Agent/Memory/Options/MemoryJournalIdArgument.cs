using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;

internal sealed class MemoryJournalIdArgument : Argument<string?>
{
    public MemoryJournalIdArgument() : base("journal-id")
    {
        Description = "The journal entry ID. Omit to list unpromoted candidates";
        Arity = ArgumentArity.ZeroOrOne;

        // Rejects anything that is not a well-formed id.
        Validators.Add(result =>
        {
            var id = result.GetValue(this);

            if (id is not null && !MemoryId.IsValid(id))
            {
                result.AddError($"'{id}' is not a valid journal entry ID.");
            }
        });
    }
}
