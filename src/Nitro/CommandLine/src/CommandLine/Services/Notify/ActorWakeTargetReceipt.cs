using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The observed status, offered and accepted generations, and diagnostic for a wake target.
/// An outcome that could not be recorded is reported as pending with null generations and diagnostic.
/// </summary>
internal sealed record ActorWakeTargetReceipt(
    AgentSessionGeneration Target,
    string Status,
    long? OfferedGeneration,
    long? AcceptedGeneration,
    string? LastError);
