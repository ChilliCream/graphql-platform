using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing.InMemory
{
    public class SessionManager
        : ISessionCreator
    {
        private readonly ConcurrentDictionary<string, string> _sessions =
            new ConcurrentDictionary<string, string>();

        public ValueTask<string> CreateSessionAsync(CancellationToken cancellationToken = default)
        {
            string sessionId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            _sessions.TryAdd(sessionId, sessionId);
            return new ValueTask<string>(sessionId);
        }

        public void RemoveSession(string sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
        }

        public void ValidateSession(string sessionId)
        {
            if (!_sessions.ContainsKey(sessionId))
            {
                throw new ArgumentException(
                    $"The specified session id `{sessionId}` is not value.",
                    nameof(sessionId));
            }
        }
    }
}
