namespace HotChocolate.StarWars.Models;

/// <summary>
/// A starship in the Star Wars universe.
/// </summary>
public class Starship(string id, string name, double length)
{
    /// <summary>
    /// The Id of the starship.
    /// </summary>
    public string Id { get; set; } = id;

    /// <summary>
    /// The name of the starship.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// The length of the starship.
    /// </summary>
    public double Length { get; set; } = length;
}
