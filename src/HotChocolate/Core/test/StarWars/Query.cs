using System.Text.Json;
using HotChocolate.Resolvers;
using HotChocolate.StarWars.Data;
using HotChocolate.StarWars.Models;

namespace HotChocolate.StarWars;

public class Query
{
    private readonly CharacterRepository _repository;

    public Query(CharacterRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Retrieve a hero by a particular Star Wars episode.
    /// </summary>
    /// <param name="episode">The episode to look up by.</param>
    /// <returns>The character.</returns>
    public ICharacter GetHero(Episode episode)
    {
        return _repository.GetHero(episode);
    }

    /// <summary>
    /// Retrieve a hero by a particular their traits.
    /// </summary>
    /// <param name="traits">The traits to look up by.</param>
    /// <returns>The character.</returns>
    public ICharacter? GetHeroByTraits(JsonElement traits)
    {
        return _repository.GetHeroByTraits(traits);
    }

    /// <summary>
    /// Retrieve a heros by a particular Star Wars episodes.
    /// </summary>
    /// <param name="episodes">The episode to look up by.</param>
    /// <returns>The character.</returns>
    public IReadOnlyList<ICharacter> GetHeroes(IReadOnlyList<Episode> episodes)
    {
        var list = new List<ICharacter>();

        foreach (var episode in episodes)
        {
            list.Add(_repository.GetHero(episode));
        }

        return list;
    }

    /// <summary>
    /// Gets a human by Id.
    /// </summary>
    /// <param name="id">The Id of the human to retrieve.</param>
    /// <returns>The human.</returns>
    public Human? GetHuman(string id)
    {
        return _repository.GetHuman(id);
    }

    /// <summary>
    /// Get a particular droid by Id.
    /// </summary>
    /// <param name="id">The Id of the droid.</param>
    /// <returns>The droid.</returns>
    public Droid? GetDroid(string id)
    {
        return _repository.GetDroid(id);
    }

    public IEnumerable<ICharacter> GetCharacter(string[] characterIds, IResolverContext context)
    {
        foreach (var characterId in characterIds)
        {
            var character = _repository.GetCharacter(characterId);

            if (character is null)
            {
                context.ReportError(
                    "Could not resolve a character for the " +
                    $"character-id {characterId}.");
            }
            else
            {
                yield return character;
            }
        }
    }

    public IEnumerable<object> Search(string text)
    {
        return _repository.Search(text);
    }
}
