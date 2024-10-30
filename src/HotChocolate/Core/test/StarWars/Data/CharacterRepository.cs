using System.Text.Json;
using HotChocolate.StarWars.Models;

namespace HotChocolate.StarWars.Data;

public class CharacterRepository
{
    private Dictionary<string, ICharacter> _characters;
    private Dictionary<string, Starship> _starships;

    public CharacterRepository()
    {
        _characters = CreateCharacters().ToDictionary(t => t.Id);
        _starships = CreateStarships().ToDictionary(t => t.Id);
    }

    public ICharacter GetHero(Episode episode)
    {
        if (episode == Episode.Empire)
        {
            return _characters["1000"];
        }
        return _characters["2001"];
    }

    public ICharacter? GetHeroByTraits(JsonElement traits)
    {
        return _characters.Values.FirstOrDefault(z => z.Traits?.ToString().Equals(traits.ToString()) == true);
    }

    public ICharacter? GetCharacter(string id)
    {
        if (_characters.TryGetValue(id, out var c))
        {
            return c;
        }
        return null;
    }

    public Human? GetHuman(string id)
    {
        if (_characters.TryGetValue(id, out var c)
            && c is Human h)
        {
            return h;
        }
        return null;
    }

    public Droid? GetDroid(string id)
    {
        if (_characters.TryGetValue(id, out var c)
            && c is Droid d)
        {
            return d;
        }
        return null;
    }

    public IEnumerable<object> Search(string text)
    {
        var filteredCharacters = _characters.Values
            .Where(t => t.Name.Contains(text));

        foreach (var character in filteredCharacters)
        {
            yield return character;
        }

        var filteredStarships = _starships.Values
            .Where(t => t.Name.Contains(text));

        foreach (var starship in filteredStarships)
        {
            yield return starship;
        }
    }

    private static IEnumerable<ICharacter> CreateCharacters()
    {
        yield return new Human(
            id: "1000",
            name: "Luke Skywalker",
            friends: new[] { "1002", "1003", "2000", "2001", },
            appearsIn: new[] { Episode.NewHope, Episode.Empire, Episode.Jedi, },
            homePlanet: "Tatooine",
            traits: JsonSerializer.SerializeToElement(new { lastJedi = true, }));

        yield return new Human(
            id: "1001",
            name: "Darth Vader",
            friends: new[] { "1004", },
            appearsIn: new[] { Episode.NewHope, Episode.Empire, Episode.Jedi, },
            homePlanet: "Tatooine",
            traits: JsonSerializer.SerializeToElement(new { theChosenOne = true, }));

        yield return new Human(
            id: "1002",
            name: "Han Solo",
            friends: new[] { "1000", "1003", "2001", },
            appearsIn: new[] { Episode.NewHope, Episode.Empire, Episode.Jedi, },
            traits: JsonSerializer.SerializeToElement(new { hanShot = "first", }));

        yield return new Human(
            id: "1003",
            name: "Leia Organa",
            friends: new[] { "1000", "1002", "2000", "2001", },
            appearsIn: new[] { Episode.NewHope, Episode.Empire, Episode.Jedi, },
            homePlanet: "Alderaan",
            traits: JsonSerializer.SerializeToElement(new { brother = "luke", }));

        yield return new Human(
            id: "1004",
            name: "Wilhuff Tarkin",
            friends: new[] { "1001", },
            appearsIn: new[] { Episode.NewHope, });

        yield return new Droid(
            id: "2000",
            name: "C-3PO",
            friends: new[] { "1000", "1002", "1003", "2001", },
            appearsIn: new[] { Episode.NewHope, Episode.Empire, Episode.Jedi, },
            primaryFunction: "Protocol",
            traits: JsonSerializer.SerializeToElement(new { annoying = true, }));

        yield return new Droid(
            id: "2001",
            name: "R2-D2",
            friends: new[] { "1000", "1002", "1003", },
            appearsIn: new[] { Episode.NewHope, Episode.Empire, Episode.Jedi, },
            primaryFunction: "Astromech",
            traits: JsonSerializer.SerializeToElement(new { rockets = true, }));
    }

    private static IEnumerable<Starship> CreateStarships()
    {
        yield return new Starship(
            id: "3000",
            name: "TIE Advanced x1",
            length: 9.2);
    }
}
