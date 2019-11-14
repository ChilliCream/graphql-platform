using System;
using System.Linq;

namespace StrawberryShake.Http.Subscriptions
{
    public interface IMessagePipeline
        : IAsyncDisposable
    {
        void Start();
    }
}
