using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    public class GetUser
        : IGetUser
    {
        public GetUser(
            IUser? user)
        {
            User = user;
        }

        public IUser? User { get; }
    }
}
