using System;

namespace HotChocolate
{
    public readonly struct ResolverResult<TValue>
        : IResolverResult<TValue>
    {
        public ResolverResult(string errorMessage)
        {
            Value = default;
            ErrorMessage = errorMessage
                ?? throw new ArgumentNullException(nameof(errorMessage));
            IsError = true;
        }

        public ResolverResult(TValue value)
        {
            Value = default;
            ErrorMessage = null;
            IsError = false;
        }

        public TValue Value { get; }

        public string ErrorMessage { get; }

        public bool IsError { get; }

        object IResolverResult.Value => Value;
    }
}
