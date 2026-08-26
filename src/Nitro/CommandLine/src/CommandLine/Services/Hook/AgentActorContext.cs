namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal static class AgentActorContext
{
    public static string Format(string actor, string role)
    {
        var context =
            $"Your Nitro actor name is \"{actor}\". Pass this name to the `--actor` option "
            + "to act under this actor explicitly.";

        return role.Length == 0 ? context : $"{context}\nYour Nitro role is \"{role}\".";
    }

    public static string Combine(string actorContext, string? mailContext)
        => mailContext is null ? actorContext : $"{actorContext}\n\n{mailContext}";
}
