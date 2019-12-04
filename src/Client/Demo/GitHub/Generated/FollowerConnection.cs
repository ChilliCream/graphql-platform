using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "0.0.0.0")]
    public class FollowerConnection
        : IFollowerConnection
    {
        public FollowerConnection(
            int totalCount)
        {
            TotalCount = totalCount;
        }

        public int TotalCount { get; }
    }
}
