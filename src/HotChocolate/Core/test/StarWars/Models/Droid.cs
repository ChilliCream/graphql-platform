using System.Text.Json;

namespace HotChocolate.StarWars.Models;

/// <summary>
/// A droid in the Star Wars universe.
/// </summary>
public class Droid(
    string id,
    string name,
    IReadOnlyList<string> friends,
    IReadOnlyList<Episode> appearsIn,
    string? primaryFunction = null,
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
    /// The droid's primary function.
    /// </summary>
    public string? PrimaryFunction { get; set; } = primaryFunction;

    /// <inheritdoc />
    public double Height { get; } = height;

    /// <inheritdoc />
    public JsonElement? Traits { get; set; } = traits;
}
