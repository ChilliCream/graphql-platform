using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    public interface IUser
    {
        string? Name { get; }

        string? Company { get; }

        System.DateTimeOffset CreatedAt { get; }

        IFollowerConnection Followers { get; }
    }
}
