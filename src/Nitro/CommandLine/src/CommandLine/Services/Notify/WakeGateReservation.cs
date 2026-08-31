using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// One target's held reservation: the session gate and the shared lease
/// slot a dispatch attempt claimed together, and the attempt id that fences
/// both.
/// </summary>
internal sealed record WakeGateReservation(AgentSessionGeneration Target, string AttemptId, int Slot);
