using System.Text.Json;

namespace HotChocolate.StarWars.Models;

/// <summary>
/// A human character in the Star Wars universe.
/// </summary>
public class Human(
    string id,
    string name,
    IReadOnlyList<string> friends,
    IReadOnlyList<Episode> appearsIn,
    string? homePlanet = null,
    double height = 1.72d,
    JsonElement? traits = null)
    : ICharacter
{
    /// <inheritdoc />
    public string Id { get; set; } = id;

    /// <inheritdoc />
    public string Name { get; set; } = name;

    /// <inheritdoc />
    public IReadOnlyList<string> Friends { get; set; } = friends;

    /// <inheritdoc />
    public IReadOnlyList<Episode> AppearsIn { get; set; } = appearsIn;

    /// <summary>
    /// The planet the character is originally from.
    /// </summary>
    public string? HomePlanet { get; set; } = homePlanet;

    /// <inheritdoc />
    public double Height { get; } = height;

    /// <inheritdoc />
    public JsonElement? Traits { get; set; } = traits;
}
