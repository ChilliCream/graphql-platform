#if FUSION_ASPIRE
namespace HotChocolate.Fusion.Aspire;
#else
namespace ChilliCream.Nitro.CommandLine.Services.Sessions;
#endif

internal sealed class Workspace(string id, string name)
{
    public string Id { get; set; } = id;

    public string Name { get; set; } = name;
}
