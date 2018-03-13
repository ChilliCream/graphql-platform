using System;
using System.Collections;
using System.Collections.Generic;

namespace Prometheus.Abstractions
{
    public class ReadOnlySet<T>
        : IReadOnlySet<T>
    {
        private readonly HashSet<T> _set;

        public ReadOnlySet(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _set = new HashSet<T>(items);
        }

        public ReadOnlySet(IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            _set = new HashSet<T>(items, equalityComparer);
        }

        public int Count => _set.Count;

        public bool Contains(T item)
        {
            return _set.Contains(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}