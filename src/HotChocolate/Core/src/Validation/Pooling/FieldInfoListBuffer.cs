using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
        private readonly int _max = 16;
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
            if (_index < _max)
            {
                list = _buffer[_index++];
                return true;
            }

            list = null;
            return false;
        }

        public void Reset()
        {
            if (_index > 0)
            {
                for (int i = 0; i < _index; i++)
                {
                    _buffer[i].Clear();
                }
            }
            _index = 0;
        }
    }
}
