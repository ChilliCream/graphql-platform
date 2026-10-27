namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// One agent's held reservation: the ping gate and the shared lease slot a
/// dispatch attempt claimed together, and the attempt id that fences both.
/// </summary>
internal sealed record WakeGateReservation(string Target, string AttemptId, int Slot);
