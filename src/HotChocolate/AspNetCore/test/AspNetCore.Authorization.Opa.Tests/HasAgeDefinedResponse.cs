namespace HotChocolate.AspNetCore.Authorization;

public class HasAgeDefinedResponse
{
    public bool Allow { get; set; } = default!;

    public Claims Claims { get; set; } = default!;
}
