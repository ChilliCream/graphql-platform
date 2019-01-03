using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    internal sealed class ErrorCollection
        : ICollection<IError>
    {
        private readonly List<IError> _errors = new List<IError>();

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        public void Add(IError item)
        {
            lock (_errors)
            {
                _errors.Add(item);
                Count = _errors.Count;
            }
        }

        public bool Remove(IError item)
        {
            lock (_errors)
            {
                bool result = _errors.Remove(item);
                Count = _errors.Count;
                return result;
            }
        }

        public void Clear()
        {
            lock (_errors)
            {
                _errors.Clear();
                Count = 0;
            }
        }

        public bool Contains(IError item)
        {
            lock (_errors)
            {
                return _errors.Contains(item);
            }
        }

        public void CopyTo(IError[] array, int arrayIndex)
        {
            lock (_errors)
            {
                _errors.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<IError> GetEnumerator()
        {
            return _errors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
