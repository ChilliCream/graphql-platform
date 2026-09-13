using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal static class AgentEffectiveRole
{
    public static async Task<string> ResolveAsync(
        string sessionRole,
        string actor,
        IAgentRegistry agentRegistry,
        CancellationToken cancellationToken)
    {
        if (sessionRole.Length > 0)
        {
            return sessionRole;
        }

        try
        {
            return (await agentRegistry.GetAsync(actor, cancellationToken))?.Role ?? string.Empty;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (AgentWorkspaceSchemaMismatchException)
        {
            throw;
        }
        catch
        {
            return string.Empty;
        }
    }
}
