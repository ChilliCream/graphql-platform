using System;

namespace HotChocolate.Runtime
{
    public interface ISessionManager
        : IDisposable
    {
        ISession CreateSession(IServiceProvider requestServices);
    }
}
