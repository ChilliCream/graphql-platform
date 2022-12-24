using System;

namespace HotChocolate.AspNetCore.Authorization;

public sealed class Policy
{
    public string Path { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
