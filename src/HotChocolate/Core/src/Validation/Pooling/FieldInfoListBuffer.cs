using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Utilities;

namespace HotChocolate.Validation
{
    internal sealed class FieldInfoListBuffer
    {
        private readonly List<FieldInfo>[] _buffer = new List<FieldInfo>[]
        {
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
            new List<FieldInfo>(),
        };
        private int _index = 0;

        public IList<FieldInfo> Pop()
        {
            if (TryPop(out IList<FieldInfo>? list))
            {
                return list;
            }
            throw new InvalidOperationException("Buffer is used up.");
        }

        public bool TryPop([NotNullWhen(true)] out IList<FieldInfo>? list)
        {
            if (_index < _buffer.Length)
            {
                list = _buffer[_index++];
                return true;
            }

            list = null;
            return false;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}
