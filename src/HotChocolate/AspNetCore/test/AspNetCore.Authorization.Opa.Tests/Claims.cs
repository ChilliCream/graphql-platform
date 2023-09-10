namespace HotChocolate.AspNetCore.Authorization;

public class Claims
{
    public string Birthdate { get; set; } = default!;

    public long Iat { get; set; }

    public string Name { get; set; } = default!;

    public string Sub { get; set; } = default!;
}
