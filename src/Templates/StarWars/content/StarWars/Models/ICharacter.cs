using System.Collections.Generic;

namespace StarWars.Models
{
    public interface ICharacter
    {
        string Id { get; }

        string Name { get; }

        IReadOnlyList<string> Friends { get; }

        IReadOnlyList<Episode> AppearsIn { get; }

        double Height { get; }
    }
}
