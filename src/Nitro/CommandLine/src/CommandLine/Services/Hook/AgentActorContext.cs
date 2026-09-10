namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The one line a session-start hook injects to tell an agent which actor it
/// acts as. Only session start announces it: Claude Code fires that event on
/// startup, resume, clear, compact, and fork, so every point a session could
/// have lost the name re-announces it, and no other event has to repeat it.
/// </summary>
internal static class AgentActorContext
{
    public static string Format(string actor, string role)
    {
        var context =
            $"Your Nitro actor name is \"{actor}\". Pass this name to the `--actor` option "
            + "to act under this actor explicitly.";

        return role.Length == 0 ? context : $"{context}\nYour Nitro role is \"{role}\".";
    }
}
