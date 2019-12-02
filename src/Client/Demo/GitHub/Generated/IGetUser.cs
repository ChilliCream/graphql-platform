using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    public interface IGetUser
    {
        IUser? User { get; }
    }
}
