using System.Collections.Generic;

namespace HotChocolate.Runtime
{
    internal class UserStateManager<TKey>
    {
        private readonly object _sync = new object();
        private readonly Cache<StateObjectCollection<TKey>> _cache;
        private readonly Dictionary<StateObjectCollection<TKey>, int> _inUse =
            new Dictionary<StateObjectCollection<TKey>, int>();
        private readonly HashSet<StateObjectCollection<TKey>> _remove =
            new HashSet<StateObjectCollection<TKey>>();

        public UserStateManager(int size)
        {
            _cache = new Cache<StateObjectCollection<TKey>>(
                size < 10 ? 10 : size);
            _cache.RemovedEntry += (s, e) =>
            {
                lock (_sync)
                {
                    _remove.Add(e.Value);
                }
            };
        }

        public StateObjectCollection<TKey> CreateUserState(
            string userKey)
        {
            StateObjectCollection<TKey> state = GetOrCreateUserState(userKey);

            lock (_sync)
            {
                if (_inUse.TryGetValue(state, out int count))
                {
                    _inUse[state] = ++count;
                }
                else
                {
                    _inUse[state] = 1;
                }
            }

            return state;
        }

        public void FinalizeUserState(StateObjectCollection<TKey> state)
        {
            lock (_sync)
            {
                if (_inUse.TryGetValue(state, out int count))
                {
                    count--;
                    if (count == 0)
                    {
                        _inUse.Remove(state);

                        if (_remove.Contains(state))
                        {
                            state.Dispose();
                            _remove.Remove(state);
                        }
                    }
                    else
                    {
                        _inUse[state] = count;
                    }
                }
            }
        }

        private StateObjectCollection<TKey> GetOrCreateUserState(
            string userKey)
        {
            return _cache.GetOrCreate(userKey,
                () => new StateObjectCollection<TKey>(ExecutionScope.User));
        }
    }
}
