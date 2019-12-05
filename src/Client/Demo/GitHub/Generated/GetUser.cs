using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "0.0.0.0")]
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
