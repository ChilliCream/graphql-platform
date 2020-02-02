using System;

namespace GreenDonut
{
    /// <summary>
    /// A wrapper for a single value which could contain a valid value or any
    /// error.
    /// </summary>
    /// <typeparam name="TValue">A value type.</typeparam>
    public struct Result<TValue>
        : IEquatable<Result<TValue>>
    {
        /// <summary>
        /// Gets an error if <see cref="IsError"/> is <c>true</c>;
        /// otherwise <c>null</c>.
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the result is an error.
        /// </summary>
        public bool IsError { get; private set; }

        /// <summary>
        /// Gets the value. If <see cref="IsError"/> is <c>true</c>, returns
        /// <c>null</c> or <c>default</c> depending on its type.
        /// </summary>
        public TValue Value { get; private set; }

        /// <inheritdoc />
        public bool Equals(Result<TValue> other)
        {
            return IsError == other.IsError &&
                Error == other.Error &&
                Equals(Value, other.Value);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is Result<TValue> result && Equals(result);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (IsError)
                ? Error.GetHashCode()
                : Value?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Creates a new error result.
        /// </summary>
        /// <param name="error">An arbitrary error.</param>
        /// <returns>An error result.</returns>
        public static Result<TValue> Reject(Exception error)
        {
            return error;
        }

        /// <summary>
        /// Creates a new value result.
        /// </summary>
        /// <param name="value">An arbitrary value.</param>
        /// <returns>A value result.</returns>
        public static Result<TValue> Resolve(TValue value)
        {
            return value;
        }

        /// <summary>
        /// Creates a new error result.
        /// </summary>
        /// <param name="error">An arbitrary error.</param>
        public static implicit operator Result<TValue>(Exception error)
        {
            if (error == null)
            {
                return new Result<TValue>
                {
                    IsError = false
                };
            }

            return new Result<TValue>
            {
                Error = error,
                IsError = true
            };
        }

        /// <summary>
        /// Creates a new value result.
        /// </summary>
        /// <param name="value">An arbitrary value.</param>
        public static implicit operator Result<TValue>(TValue value)
        {
            return new Result<TValue>
            {
                Value = value,
                IsError = false
            };
        }

        /// <summary>
        /// Extracts the error from a result.
        /// </summary>
        /// <param name="result">An arbitrary result.</param>
        public static implicit operator Exception(Result<TValue> result)
        {
            return result.Error;
        }

        /// <summary>
        /// Extracts the value from a result.
        /// </summary>
        /// <param name="result">An arbitrary result.</param>
        public static implicit operator TValue(Result<TValue> result)
        {
            return result.Value;
        }
    }
}
