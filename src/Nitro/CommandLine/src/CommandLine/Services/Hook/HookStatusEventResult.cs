namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record HookStatusEventResult(
    string Event, 
    HookStatusOutcome Outcome, 
    string? InstalledCommand);
