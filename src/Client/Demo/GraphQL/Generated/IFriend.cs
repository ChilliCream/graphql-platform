﻿using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public interface IFriend
    {
        IReadOnlyList<IHasName> Nodes { get; }
    }
}
