using System;

namespace HotChocolate
{
    public readonly struct ResolverResult<TValue>
        : IResolverResult<TValue>
    {
        private ResolverResult(string errorMessage)
        {
            Value = default;
            ErrorMessage = errorMessage
                ?? throw new ArgumentNullException(nameof(errorMessage));
            IsError = true;
        }

        private ResolverResult(TValue value)
        {
            Value = value;
            ErrorMessage = null;
            IsError = false;
        }

        public TValue Value { get; }

        public string ErrorMessage { get; }

        public bool IsError { get; }

        object IResolverResult.Value => Value;

        public static ResolverResult<TValue> CreateError(string errorMessage)
        {
            return new ResolverResult<TValue>(errorMessage);
        }

        public static ResolverResult<TValue> CreateValue(TValue value)
        {
            return new ResolverResult<TValue>(value);
        }
    }
}
