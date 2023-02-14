namespace HotChocolate.Data.Raven.Test;

public class Car
{
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Engine? Engine { get; set; }
}
