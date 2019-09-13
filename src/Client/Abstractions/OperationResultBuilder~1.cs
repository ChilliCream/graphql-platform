using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public class OperationResultBuilder<T>
    {
        private OperationResult<T> _result = new OperationResult<T>();
        private bool _dirty;

        public OperationResultBuilder<T> SetData(T data)
        {
            CheckIfDirty();
            _result.Data = data;
            return this;
        }

        public OperationResultBuilder<T> SetErrors(
            IReadOnlyList<IError> errors)
        {
            CheckIfDirty();
            _result.Errors = errors;
            return this;
        }

        public OperationResultBuilder<T> SetExtensions(
            IReadOnlyDictionary<string, object> extensions)
        {
            CheckIfDirty();
            _result.Extensions = extensions;
            return this;
        }

        public IOperationResult<T> Build()
        {
            if (Equals(_result.Data, default(T))
                && (_result.Errors is null || _result.Errors.Count == 0))
            {
                throw new InvalidOperationException(
                    "An operation result must either have data or errors or both.");
            }

            _dirty = true;
            return _result;
        }

        public static OperationResultBuilder<T> New() =>
            new OperationResultBuilder<T>();

        private void CheckIfDirty()
        {
            if (_dirty)
            {
                OperationResult<T> source = _result;
                _result = new OperationResult<T>();
                CopyResult(source);
                _dirty = false;
            }
        }

        private void CopyResult(OperationResult<T> source)
        {
            _result.Data = source.Data;
            _result.Errors = source.Errors;
            _result.Extensions = source.Extensions;
        }
    }
}
