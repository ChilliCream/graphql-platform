using System;
using System.Collections.Generic;
using System.Linq;
using StarWars.Characters;

namespace StarWars.Repositories
{
    public class CharacterRepository : ICharacterRepository
    {
        private Dictionary<int, ICharacter> _characters;
        private Dictionary<int, Starship> _starships;

        public CharacterRepository()
        {
            _characters = CreateCharacters().ToDictionary(t => t.Id);
            _starships = CreateStarships().ToDictionary(t => t.Id);
        }

        public IQueryable<ICharacter> GetCharacters() =>
            _characters.Values.AsQueryable();

        public IEnumerable<ICharacter> GetCharacters(int[] ids)
        {
            foreach (int id in ids)
            {
                if (_characters.TryGetValue(id, out ICharacter? c))
                {
                    yield return c;
                }
            }
        }

        public ICharacter GetHero(Episode episode)
        {
            if (episode == Episode.Empire)
            {
                return _characters[1000];
            }
            return _characters[2001];
        }

        public IEnumerable<ISearchResult> Search(string text)
        {
            IEnumerable<ICharacter> filteredCharacters = _characters.Values
                .Where(t => t.Name.Contains(text,
                    StringComparison.OrdinalIgnoreCase));

            foreach (ICharacter character in filteredCharacters)
            {
                yield return character;
            }

            IEnumerable<Starship> filteredStarships = _starships.Values
                .Where(t => t.Name.Contains(text,
                    StringComparison.OrdinalIgnoreCase));

            foreach (Starship starship in filteredStarships)
            {
                yield return starship;
            }
        }

        private static IEnumerable<ICharacter> CreateCharacters()
        {
            yield return new Human
            (
                1000,
                "Luke Skywalker",
                new[] { 1002, 1003, 2000, 2001 },
                new[] { Episode.NewHope, Episode.Empire, Episode.Jedi },
                "Tatooine"
            );

            yield return new Human
            (
                1001,
                "Darth Vader",
                new[] { 1004 },
                new[] { Episode.NewHope, Episode.Empire, Episode.Jedi },
                "Tatooine"
            );

            yield return new Human
            (
                1002,
                "Han Solo",
                new[] { 1000, 1003, 2001 },
                new[] { Episode.NewHope, Episode.Empire, Episode.Jedi }
            );

            yield return new Human
            (
                1003,
                "Leia Organa",
                new[] { 1000, 1002, 2000, 2001 },
                new[] { Episode.NewHope, Episode.Empire, Episode.Jedi },
                "Alderaan"
            );

            yield return new Human
            (
                1004,
                "Wilhuff Tarkin",
                new[] { 1001 },
                new[] { Episode.NewHope }
            );

            yield return new Droid
            (
                2000,
                "C-3PO",
                new[] { 1000, 1002, 1003, 2001 },
                new[] { Episode.NewHope, Episode.Empire, Episode.Jedi },
                "Protocol"
            );

            yield return new Droid
            (
                2001,
                "R2-D2",
                new[] { 1000, 1002, 1003 },
                new[] { Episode.NewHope, Episode.Empire, Episode.Jedi },
                "Astromech"
            );
        }

        private static IEnumerable<Starship> CreateStarships()
        {
            yield return new Starship
            (
                3000,
                "TIE Advanced x1",
                 9.2
            );
        }
    }
}
