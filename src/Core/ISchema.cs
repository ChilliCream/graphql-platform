using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate
{
    public interface ISchema
        : IEnumerable<IType>
    {
        IType GetType(string name);
        T GetType<T>(string name) where T : IType;
    }

    public class Schema
        : ISchema
    {

        public IType GetType(string name)
        {
            throw new NotImplementedException();
        }

        public T GetType<T>(string name) where T : IType
        {
            throw new NotImplementedException();
        }

        public IEnumerator<IType> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}