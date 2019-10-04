using System.Collections.Generic;

namespace StrawberryShake
{

    public class OperationResultBuilder<T>
        : OperationResultBuilder
        where T : class
    {
        public OperationResultBuilder()
            : base(typeof(T))
        {
        }

        public OperationResultBuilder(IOperationResult result)
            : base(result)
        {
        }

        protected new T? Data => (T?)base.Data;

        public OperationResultBuilder<T> SetData(T data)
        {
            base.SetData(data);
            return this;
        }

        public new OperationResultBuilder<T> AddErrors(
            IEnumerable<IError> errors)
        {
            base.AddErrors(errors);
            return this;
        }

        public new OperationResultBuilder<T> AddError(IError error)
        {
            base.AddError(error);
            return this;
        }

        public new OperationResultBuilder<T> ClearErrors()
        {
            base.ClearErrors();
            return this;
        }

        public new OperationResultBuilder<T> AddExtensions(
            IEnumerable<KeyValuePair<string, object?>> extensions)
        {
            base.AddExtensions(extensions);
            return this;
        }

        public new OperationResultBuilder<T> AddExtension(
            string key, object? value)
        {
            base.AddExtension(key, value);
            return this;
        }

        public new OperationResultBuilder<T> SetExtension(
            string key, object? value)
        {
            base.SetExtension(key, value);
            return this;
        }

        public new OperationResultBuilder<T> RemoveExtension(string key)
        {
            base.RemoveExtension(key);
            return this;
        }

        public new OperationResultBuilder<T> ClearExtensions()
        {
            base.ClearExtensions();
            return this;
        }

        public new OperationResultBuilder<T> ClearAll()
        {
            base.ClearAll();
            return this;
        }

        public new IOperationResult<T> Build()
        {
            Validate();
            SetDirty();
            return new OperationResult<T>
            (
                Data,
                Errors,
                Extensions
            );
        }

        protected override IOperationResult CreateResult()
        {
            return new OperationResult<T>
            (
                Data,
                Errors,
                Extensions
            );
        }
    }
}
