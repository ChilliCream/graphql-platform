using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal sealed class ResultHelper : IResultHelper
    {
        public ResultMap RentResultMap(int count)
        {
            throw new System.NotImplementedException();
        }

        public ResultMapList RentResultMapList()
        {
            throw new System.NotImplementedException();
        }

        public ResultList RentResultList()
        {
            throw new System.NotImplementedException();
        }

        public void Return(ResultMap rentedObject)
        {
            throw new System.NotImplementedException();
        }

        public void Return(ResultMapList rentedObject)
        {
            throw new System.NotImplementedException();
        }
        
        public void Return(ResultList rentedObject)
        {
            throw new System.NotImplementedException();
        }

        public void SetData(IResultMap resultMap)
        {
            throw new System.NotImplementedException();
        }

        public void AddError(IError error, FieldNode? selection = null)
        {
            throw new System.NotImplementedException();
        }

        public void AddErrors(IEnumerable<IError> errors, FieldNode? selection = null)
        {
            throw new System.NotImplementedException();
        }

        public void AddNonNullViolation(FieldNode selection, Path path, IResultMap parent)
        {
            throw new System.NotImplementedException();
        }

        public IReadOnlyQueryResult BuildResult()
        {
            throw new System.NotImplementedException();
        }
    }
}