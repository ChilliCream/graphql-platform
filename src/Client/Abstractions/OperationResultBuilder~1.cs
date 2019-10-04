using System.Linq;
using System;

namespace StrawberryShake
{

    public class OperationResultBuilder<T>
        : IOperationResultBuilder
    {
        private readonly ConstructorInfo _createResult;
        private readonly Type _resultType;
        private object? _data;
        private List<IError>? _errors;
        private Dictionary<string, object?>? _extensions;
        private bool _dirty;

        public OperationResultBuilder(Type resultType)
        {
            if (resultType is null)
            {
                throw new ArgumentNullException(nameof(resultType));
            }

            _createResult = typeof(OperationResult<>)
                .MakeGenericType(resultType)
                .GetConstructors()[0];
            _resultType = resultType;
        }

        public OperationResultBuilder(IOperationResult result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            _createResult = typeof(OperationResult<>)
                .MakeGenericType(result.ResultType)
                .GetConstructors()[0];
            _resultType = result.ResultType;

            _data = result.Data;
            _errors = result.Errors.Count == 0
                ? null
                : result.Errors.ToList();
            _extensions = result.Extensions.Count == 0
                ? null
                : result.Extensions.ToDictionary(t => t.Key, t => t.Value);
        }

        public OperationResultBuilder SetData(object data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _data = data;
            return this;
        }

        public OperationResultBuilder AddErrors(
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

        public OperationResultBuilder AddError(IError error)
        {
            CheckIfDirty();

            if (_errors is null)
            {
                _errors = new List<IError>();
            }

            _errors.Add(error);

            return this;
        }

        public OperationResultBuilder ClearErrors()
        {
            CheckIfDirty();
            _errors = null;
            return this;
        }

        public OperationResultBuilder AddExtensions(
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

        public OperationResultBuilder AddExtension(
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

        public OperationResultBuilder SetExtension(string key, object? value)
        {
            CheckIfDirty();

            if (_extensions is null)
            {
                _extensions = new Dictionary<string, object?>();
            }
            _extensions[key] = value;

            return this;
        }

        public OperationResultBuilder RemoveExtension(string key)
        {
            CheckIfDirty();

            if (_extensions is null)
            {
                _extensions = new Dictionary<string, object?>();
            }
            _extensions.Remove(key);

            return this;
        }

        public OperationResultBuilder ClearExtension()
        {
            CheckIfDirty();
            _extensions = null;
            return this;
        }

        public OperationResultBuilder ClearAll()
        {
            _data = null;
            _errors = null;
            _extensions = null;
            _dirty = false;
            return this;
        }

        public IOperationResult Build()
        {
            if (_data is null && (_errors is null || _errors.Count == 0))
            {
                throw new InvalidOperationException(
                    "An operation result must either have data or errors or both.");
            }

            _dirty = true;
            return (IOperationResult)_createResult.Invoke
            (
                new object?[]
                {
                    _data,
                    _errors,
                    _extensions
                }
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

        public static OperationResultBuilder New(Type resultType) =>
            new OperationResultBuilder(resultType);

        public static OperationResultBuilder FromResult(
            IOperationResult result) =>
            new OperationResultBuilder(result);

        public static OperationResultBuilder<T> New<T>()
            where T : class =>
            new OperationResultBuilder<T>();

        public static OperationResultBuilder<T> FromResult<T>(
            IOperationResult<T> result)
            where T : class =>
            new OperationResultBuilder<T>(result);
    }
}
