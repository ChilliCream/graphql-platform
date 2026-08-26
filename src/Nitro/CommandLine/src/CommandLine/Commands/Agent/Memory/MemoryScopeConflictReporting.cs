using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;

/// <summary>
/// Reports a <see cref="MemoryScopeConflictException"/> the same way in
/// every read command that can hit it: a structured diagnostic in JSON
/// mode, a readable explanation otherwise, and a nonzero exit code with no
/// partial memory result in either mode.
/// </summary>
internal static class MemoryScopeConflictReporting
{
    public static int Report(
        INitroConsole console, IResultHolder resultHolder, MemoryScopeConflictException exception)
    {
        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(MemoryScopeConflictResult.Create(exception)));
            return ExitCodes.Error;
        }

        console.Error.WriteErrorLine(
            "Cross-scope duplicate memory ids found; this is invalid data:");

        foreach (var conflict in exception.Conflicts)
        {
            console.Error.WriteErrorLine(
                $"  '{conflict.Id}' exists in {string.Join(" and ", conflict.Scopes)}: "
                + string.Join(", ", conflict.Paths));
        }

        console.Error.WriteErrorLine(
            "Use an explicit --scope to inspect each copy, or run `nitro agent memory doctor`.");

        return ExitCodes.Error;
    }
}
