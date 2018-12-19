﻿using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class Human
        : ICharacter
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public IReadOnlyList<string> Friends { get; set; }

        public IReadOnlyList<Episode> AppearsIn { get; set; }

        public string HomePlanet { get; set; }

        public double Height { get; } = 1.72d;

        public Task<IReadOnlyList<Human>> GetOtherHuman(
            [DataLoader]HumanDataLoader humanDataLoader)
        {
            return humanDataLoader.LoadAsync(new[] { "1001", "1002", "9999" });
        }
    }
}
