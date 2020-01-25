using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public interface IUser
    {
        string? Name { get; }

        string? Company { get; }

        System.DateTimeOffset CreatedAt { get; }

        IFollowerConnection Followers { get; }
    }
}
