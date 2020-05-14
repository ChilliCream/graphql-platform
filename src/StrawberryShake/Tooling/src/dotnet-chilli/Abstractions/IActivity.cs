using System;
using System.Collections.Generic;
using HCError = HotChocolate.IError;

namespace StrawberryShake.Tools.Abstractions
{
    public interface IActivity : IDisposable
    {
        void WriteError(HCError error);

        void WriteErrors(IEnumerable<HCError> error);
    }
}
