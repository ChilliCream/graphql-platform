using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class GetCharacter
        : IGetCharacter
    {
        public GetCharacter(
            IReadOnlyList<IHasName> character)
        {
            Character = character;
        }

        public IReadOnlyList<IHasName> Character { get; }
    }
}
