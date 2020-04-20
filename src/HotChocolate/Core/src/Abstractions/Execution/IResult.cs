using System;

#nullable enable

namespace HotChocolate.Execution
{
    public interface IResult : IDisposable
    {
        IResultMap? Data { get; }
    }
}
