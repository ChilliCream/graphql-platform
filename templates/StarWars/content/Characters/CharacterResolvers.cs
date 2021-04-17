using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using StarWars.Repositories;

namespace StarWars.Characters
{
    /// <summary>
    /// This resolver class extends all object types implementing <see cref="ICharacter"/>.
    /// </summary>
    [ExtendObjectType(typeof(ICharacter))]
    public class CharacterResolvers
    {
        [UsePaging(typeof(InterfaceType<ICharacter>))]
        [BindMember(nameof(ICharacter.Friends))]
        public IEnumerable<ICharacter> GetFriends(
            [Parent] ICharacter character,
            [Service] ICharacterRepository repository) =>
            repository.GetCharacters(character.Friends.ToArray());
    }
}