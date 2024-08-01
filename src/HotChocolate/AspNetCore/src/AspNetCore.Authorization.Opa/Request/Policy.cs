namespace HotChocolate.AspNetCore.Authorization;

public sealed class Policy
{
    public Policy(string path, IReadOnlyList<string> roles)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));
    }

    public string Path { get; }

    public IReadOnlyList<string> Roles { get; }
}
