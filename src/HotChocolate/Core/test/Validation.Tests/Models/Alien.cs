namespace HotChocolate.Validation;

public class Alien(string name, string? homePlanet = null) : ISentient
{
    public string Name { get; set; } = name;
    public string? HomePlanet { get; set; } = homePlanet;
}
