using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;

internal sealed class MemoryCollectionOption : Option<string>
{
    public MemoryCollectionOption() : base("--collection")
    {
        Description = "The memory collection to show (curated, journal, or all)";
        Required = false;
        DefaultValueFactory = _ => MemoryCollections.Curated;
        AcceptOnlyFromAmong(MemoryCollections.Curated, MemoryCollections.Journal, MemoryCollections.All);
    }
}
