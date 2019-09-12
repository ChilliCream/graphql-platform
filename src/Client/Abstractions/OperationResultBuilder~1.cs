using System.Linq;
using System;
using System.Collections.Generic;

namespace StrawberryShake
{

    public class OperationResultBuilder<T>
        where T : class
    {
        private T? _data;
        private List<IError>? _errors;
        private Dictionary<string, object?>? _extensions;
        private bool _dirty;

        public OperationResultBuilder()
        {
        }

        public OperationResultBuilder(IOperationResult<T> result)
        {
            _data = result.Data;
            _errors = result.Errors.Count == 0
                ? null
                : result.Errors.ToList();
            _extensions = result.Extensions.Count == 0
                ? null
                : result.Extensions.ToDictionary(t => t.Key, t => t.Value);
        }

        public OperationResultBuilder<T> SetData(T data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _data = data;
            return this;
        }

        public OperationResultBuilder<T> AddErrors(
            IEnumerable<IError> errors)
        {
            CheckIfDirty();

            if (_errors is null)
            {
                _errors = new List<IError>();
            }

            _errors.AddRange(errors);

            return this;
        }

        public OperationResultBuilder<T> AddError(IError error)
        {
            CheckIfDirty();

            if (_errors is null)
            {
                _errors = new List<IError>();
            }

            _errors.Add(error);

            return this;
        }

        public OperationResultBuilder<T> ClearErrors()
        {
            CheckIfDirty();
            _errors = null;
            return this;
        }

        public OperationResultBuilder<T> AddExtensions(
            IEnumerable<KeyValuePair<string, object?>> extensions)
        {
            CheckIfDirty();

            if (_extensions is null)
            {
                _extensions = new Dictionary<string, object?>();
            }

            foreach (KeyValuePair<string, object?> item in extensions)
            {
                _extensions.Add(item.Key, item.Value);
            }

            return this;
        }

        public OperationResultBuilder<T> AddExtension(
            string key, object? value)
        {
            CheckIfDirty();

            if (_extensions is null)
            {
                _extensions = new Dictionary<string, object?>();
            }
            _extensions.Add(key, value);

            return this;
        }

        public OperationResultBuilder<T> SetExtension(string key, object? value)
        {
            CheckIfDirty();

            if (_extensions is null)
            {
                _extensions = new Dictionary<string, object?>();
            }
            _extensions[key] = value;

            return this;
        }

        public OperationResultBuilder<T> RemoveExtension(string key)
        {
            CheckIfDirty();

            if (_extensions is null)
            {
                _extensions = new Dictionary<string, object?>();
            }
            _extensions.Remove(key);

            return this;
        }

        public OperationResultBuilder<T> ClearExtension()
        {
            CheckIfDirty();
            _extensions = null;
            return this;
        }

        public IOperationResult<T> Build()
        {
            if (_data is null && (_errors is null || _errors.Count == 0))
            {
                throw new InvalidOperationException(
                    "An operation result must either have data or errors or both.");
            }

            _dirty = true;
            return new OperationResult<T>
            (
                _data,
                _errors,
                _extensions
            );
        }

        private void CheckIfDirty()
        {
            if (_dirty)
            {
                if (_errors != null)
                {
                    _errors = new List<IError>(_errors);
                }

                if (_extensions != null)
                {
                    _extensions = new Dictionary<string, object?>(_extensions);
                }
            }
        }

        public static OperationResultBuilder<T> New() =>
            new OperationResultBuilder<T>();

        public static OperationResultBuilder<T> FromResult(
            IOperationResult<T> result) =>
            new OperationResultBuilder<T>(result);
    }
}
