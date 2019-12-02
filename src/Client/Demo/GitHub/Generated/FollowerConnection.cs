using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
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
