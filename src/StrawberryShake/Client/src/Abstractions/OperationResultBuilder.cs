using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake
{
    public class OperationResultBuilder
        : IOperationResultBuilder
    {
        private readonly Type _resultType;
        private ConstructorInfo? _createResult;
        private object? _data;
        private List<IError>? _errors;
        private Dictionary<string, object?>? _extensions;
        private bool _dirty;

        public object? Data => _data;

        public IReadOnlyList<IError>? Errors => _errors;

        public IReadOnlyDictionary<string, object?>? Extensions => _extensions;

        public bool IsDataOrErrorModified { get; private set; }

        public OperationResultBuilder(Type resultType)
        {
            if (resultType is null)
            {
                throw new ArgumentNullException(nameof(resultType));
            }

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

        IOperationResultBuilder IOperationResultBuilder.SetData(object data) =>
            SetData(data);

        public OperationResultBuilder SetData(object data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _data = data;
            IsDataOrErrorModified = true;
            return this;
        }

        IOperationResultBuilder IOperationResultBuilder.AddErrors(
            IEnumerable<IError> errors) =>
            AddErrors(errors);

        public OperationResultBuilder AddErrors(
            IEnumerable<IError> errors)
        {
            CheckIfDirty();

            if (_errors is null)
            {
                _errors = new List<IError>();
            }

            _errors.AddRange(errors);
            IsDataOrErrorModified = true;

            return this;
        }

        IOperationResultBuilder IOperationResultBuilder.AddError(
            IError error) =>
            AddError(error);

        public OperationResultBuilder AddError(IError error)
        {
            CheckIfDirty();

            if (_errors is null)
            {
                _errors = new List<IError>();
            }

            _errors.Add(error);
            IsDataOrErrorModified = true;

            return this;
        }

        IOperationResultBuilder IOperationResultBuilder.ClearErrors() =>
            ClearErrors();

        public OperationResultBuilder ClearErrors()
        {
            CheckIfDirty();
            _errors = null;
            return this;
        }

        IOperationResultBuilder IOperationResultBuilder.AddExtensions(
            IEnumerable<KeyValuePair<string, object?>> extensions) =>
            AddExtensions(extensions);

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

        IOperationResultBuilder IOperationResultBuilder.AddExtension(
            string key, object? value) =>
            AddExtension(key, value);

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

        IOperationResultBuilder IOperationResultBuilder.SetExtension(
            string key, object? value) =>
            SetExtension(key, value);

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

        IOperationResultBuilder IOperationResultBuilder.RemoveExtension(
            string key) => RemoveExtension(key);

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

        IOperationResultBuilder IOperationResultBuilder.ClearExtensions() =>
            ClearExtensions();

        public OperationResultBuilder ClearExtensions()
        {
            CheckIfDirty();
            _extensions = null;
            return this;
        }

        IOperationResultBuilder IOperationResultBuilder.ClearAll() =>
            ClearAll();

        public OperationResultBuilder ClearAll()
        {
            _data = null;
            _errors = null;
            _extensions = null;
            _dirty = false;
            IsDataOrErrorModified = false;
            return this;
        }

        public IOperationResult Build()
        {
            Validate();
            SetDirty();
            return CreateResult();
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

        protected virtual void Validate()
        {
            if (_data is null && (_errors is null || _errors.Count == 0))
            {
                throw new InvalidOperationException(
                    "An operation result must either have data or errors or both.");
            }
        }

        protected virtual IOperationResult CreateResult()
        {
            if (_createResult is null)
            {
                _createResult = typeof(OperationResult<>)
                    .MakeGenericType(_resultType)
                    .GetConstructors()[0];
            }

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

        protected void SetDirty()
        {
            _dirty = true;
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
