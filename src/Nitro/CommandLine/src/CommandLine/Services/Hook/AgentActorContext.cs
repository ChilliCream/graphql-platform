namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal static class AgentActorContext
{
    public static string Format(string actor, string role)
    {
        var context = $"Your Nitro actor is `{actor}`.\n"
            + "Nitro uses it automatically when this session can be identified. "
            + "Pass `--actor <actor>` to act under another actor explicitly.\n"
            + "To change this session's actor, run "
            + "`nitro agent register --actor <actor>` from this session.";

        return role.Length == 0 ? context : $"{context}\nYour Nitro role is `{role}`.";
    }

    public static string Combine(string actorContext, string? mailContext)
        => mailContext is null ? actorContext : $"{actorContext}\n\n{mailContext}";
}
