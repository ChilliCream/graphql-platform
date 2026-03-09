using HotChocolate.StarWars.Data;
using HotChocolate.StarWars.Models;

namespace HotChocolate.StarWars.Resolvers;

public class SharedResolvers
{
    public async Task<IEnumerable<ICharacter>> GetCharacters(
        [Parent] ICharacter character,
        [Service] CharacterRepository repository)
    {
        await Task.Delay(250);

        return GetCharactersInternal(character, repository);
    }

    private IEnumerable<ICharacter> GetCharactersInternal(
        [Parent] ICharacter character,
        [Service] CharacterRepository repository)
    {
        foreach (var friendId in character.Friends)
        {
            var friend = repository.GetCharacter(friendId);
            if (friend != null)
            {
                yield return friend;
            }
        }
    }

    public Human? GetOtherHuman(
        [Parent] ICharacter character,
        [Service] CharacterRepository repository)
    {
        if (character.Friends.Count == 0)
        {
            return null;
        }
        return character.Friends
            .Select(repository.GetCharacter)
            .OfType<Human>()
            .FirstOrDefault();
    }

    public double GetHeight(Unit? unit, [Parent] ICharacter character)
        => ConvertToUnit(character.Height, unit);

    public double GetLength(Unit? unit, [Parent] Starship starship)
        => ConvertToUnit(starship.Length, unit);

    private double ConvertToUnit(double length, Unit? unit)
    {
        if (unit == Unit.Foot)
        {
            return length * 3.28084d;
        }
        return length;
    }
}
