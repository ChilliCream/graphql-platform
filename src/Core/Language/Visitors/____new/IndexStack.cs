using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language
{
    // A simple stack of objects.  Internally it is implemented as an array,
    // so Push can be O(n).  Pop is O(1).
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public class IndexStack<T>
        : IEnumerable<T>
        , ICollection
        , IReadOnlyList<T>
    {
        private T[] _array; // Storage for stack elements. Do not rename (binary serialization)
        private int _size; // Number of items in the stack. Do not rename (binary serialization)
        private int _version; // Used to keep enumerator in sync w/ collection. Do not rename (binary serialization)

        private const int DefaultCapacity = 4;

        public IndexStack()
        {
            _array = Array.Empty<T>();
        }

        // Create a stack with a specific initial capacity.  The initial capacity
        // must be a non-negative number.
        public IndexStack(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    capacity,
                    "Non-negative number required.");
            }
            _array = new T[capacity];
        }

        // Fills a Stack with the contents of a particular collection.  The items are
        // pushed onto the stack in the same order they are read by the enumerator.
        public IndexStack(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            _array = collection.ToArray();
        }

        public int Count
        {
            get { return _size; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot => this;

        public T this[int index]
        {
            get
            {
                T[] array = _array;
                if (_size <= index)
                {
                    throw new IndexOutOfRangeException();
                }
                return array[index];
            }
        }

        // Removes all Objects from the Stack.
        public void Clear()
        {
            Array.Clear(_array, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
            _size = 0;
            _version++;
        }

        public bool Contains(T item)
        {
            // Compare items using the default equality comparer

            // PERF: Internally Array.LastIndexOf calls
            // EqualityComparer<T>.Default.LastIndexOf, which
            // is specialized for different types. This
            // boosts performance since instead of making a
            // virtual method call each iteration of the loop,
            // via EqualityComparer<T>.Default.Equals, we
            // only make one virtual call to EqualityComparer.LastIndexOf.

            return _size != 0 && Array.LastIndexOf(_array, item, _size - 1) != -1;
        }

        // Copies the stack into an array.
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(arrayIndex),
                    arrayIndex,
                    "Index was out of range. Must be non-negative and less " +
                    "than the size of the collection.");
            }

            if (array.Length - arrayIndex < _size)
            {
                throw new ArgumentException(
                    "Offset and length were out of bounds for the array " +
                    "or count is greater than the number of elements from " +
                    "index to the end of the source collection.");
            }

            Debug.Assert(array != _array);
            int srcIndex = 0;
            int dstIndex = arrayIndex + _size;
            while (srcIndex < _size)
            {
                array[--dstIndex] = _array[srcIndex++];
            }
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(
                    "Only single dimensional arrays are supported " +
                    "for the requested action.",
                    nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(
                    "The lower bound of target array must be zero.",
                    nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(arrayIndex),
                    arrayIndex,
                    "Index was out of range. Must be non-negative and " +
                    "less than the size of the collection.");
            }

            if (array.Length - arrayIndex < _size)
            {
                throw new ArgumentException(
                    "Offset and length were out of bounds for the array or " +
                    "count is greater than the number of elements from index " +
                    "to the end of the source collection.");
            }

            try
            {
                Array.Copy(_array, 0, array, arrayIndex, _size);
                Array.Reverse(array, arrayIndex, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException(
                    "Target array type is not compatible with the type " +
                    "of items in the collection.",
                    nameof(array));
            }
        }

        // Returns an IEnumerator for this Stack.
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <internalonly/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public void TrimExcess()
        {
            int threshold = (int)(((double)_array.Length) * 0.9);
            if (_size < threshold)
            {
                Array.Resize(ref _array, _size);
                _version++;
            }
        }

        // Returns the top object on the stack without removing it.  If the stack
        // is empty, Peek throws an InvalidOperationException.
        public T Peek()
        {
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                ThrowForEmptyStack();
            }

            return array[size];
        }

        public bool TryPeek(out T result)
        {
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                result = default(T);
                return false;
            }
            result = array[size];
            return true;
        }

        // Pops an item from the top of the stack.  If the stack is empty, Pop
        // throws an InvalidOperationException.
        public T Pop()
        {
            int size = _size - 1;
            T[] array = _array;

            // if (_size == 0) is equivalent to if (size == -1), and this case
            // is covered with (uint)size, thus allowing bounds check elimination
            // https://github.com/dotnet/coreclr/pull/9773
            if ((uint)size >= (uint)array.Length)
            {
                ThrowForEmptyStack();
            }

            _version++;
            _size = size;
            T item = array[size];
            array[size] = default(T);     // Free memory quicker.
            return item;
        }

        public bool TryPop(out T result)
        {
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                result = default(T);
                return false;
            }

            _version++;
            _size = size;
            result = array[size];
            array[size] = default(T);
            return true;
        }

        // Pushes an item to the top of the stack.
        public void Push(T item)
        {
            int size = _size;
            T[] array = _array;

            if ((uint)size < (uint)array.Length)
            {
                array[size] = item;
                _version++;
                _size = size + 1;
            }
            else
            {
                PushWithResize(item);
            }
        }

        // Non-inline from Stack.Push to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PushWithResize(T item)
        {
            Array.Resize(ref _array, (_array.Length == 0) ? DefaultCapacity : 2 * _array.Length);
            _array[_size] = item;
            _version++;
            _size++;
        }

        // Copies the Stack to an array, in the same order Pop would return the items.
        public T[] ToArray()
        {
            if (_size == 0)
                return Array.Empty<T>();

            T[] objArray = new T[_size];
            int i = 0;
            while (i < _size)
            {
                objArray[i] = _array[_size - i - 1];
                i++;
            }
            return objArray;
        }

        private void ThrowForEmptyStack()
        {
            Debug.Assert(_size == 0);
            throw new InvalidOperationException("Stack empty.");
        }

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes",
            Justification = "not an expected scenario")]
        public struct Enumerator
            : IEnumerator<T>
            , IEnumerator
        {
            private readonly IndexStack<T> _stack;
            private readonly int _version;
            private int _index;
            private T _currentElement;

            internal Enumerator(IndexStack<T> stack)
            {
                _stack = stack;
                _version = stack._version;
                _index = -2;
                _currentElement = default(T);
            }

            public void Dispose()
            {
                _index = -1;
            }

            public bool MoveNext()
            {
                bool retval;
                if (_version != _stack._version)
                {
                    throw new InvalidOperationException(
                        "Collection was modified after the enumerator " +
                        "was instantiated.");
                }
                if (_index == -2)
                {  // First call to enumerator.
                    _index = _stack._size - 1;
                    retval = (_index >= 0);
                    if (retval)
                        _currentElement = _stack._array[_index];
                    return retval;
                }
                if (_index == -1)
                {  // End of enumeration.
                    return false;
                }

                retval = (--_index >= 0);
                if (retval)
                {
                    _currentElement = _stack._array[_index];
                }
                else
                {
                    _currentElement = default(T);
                }
                return retval;
            }

            public T Current
            {
                get
                {
                    if (_index < 0)
                        ThrowEnumerationNotStartedOrEnded();
                    return _currentElement;
                }
            }

            private void ThrowEnumerationNotStartedOrEnded()
            {
                Debug.Assert(_index == -1 || _index == -2);
                throw new InvalidOperationException(
                    _index == -2
                    ? "Enumeration has not started. Call MoveNext."
                    : "Enumeration already finished.");
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                if (_version != _stack._version)
                {
                    throw new InvalidOperationException(
                        "Collection was modified after the enumerator " +
                        "was instantiated.");
                }
                _index = -2;
                _currentElement = default(T);
            }
        }
    }
}
