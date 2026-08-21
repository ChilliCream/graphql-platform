using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryJournalIdArgument : Argument<string?>
{
    public MemoryJournalIdArgument() : base("journal-id")
    {
        Description = "The journal entry ID. Omit to list unpromoted candidates";
        Arity = ArgumentArity.ZeroOrOne;

        // Rejects anything that is not a well-formed id up front, so a
        // value like "../../x" can never reach a Path.Combine call that
        // builds a journal file path from it.
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
