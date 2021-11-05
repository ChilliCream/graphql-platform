using System;

namespace GreenDonut
{
    /// <summary>
    /// A wrapper for a single value which could contain a valid value or any
    /// error.
    /// </summary>
    /// <typeparam name="TValue">A value type.</typeparam>
    public readonly struct Result<TValue> : IEquatable<Result<TValue>>
    {
        /// <summary>
        /// Creates a new value result.
        /// </summary>
        /// <param name="value">The value.</param>
        public Result(TValue value) : this()
        {
            Error = default;
            Value = value;
            Kind = ResultKind.Value;
        }

        /// <summary>
        /// Creates a new error result.
        /// </summary>
        /// <param name="error">
        /// The error.
        /// </param>
        public Result(Exception error)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
            Value = default!;
            Kind = ResultKind.Error;
        }

        /// <summary>
        /// Gets a value indicating whether the result is an error, a value or undefined.
        /// </summary>
        public ResultKind Kind { get; }

        /// <summary>
        /// Gets the value. If <see cref="Kind"/> is <see cref="ResultKind.Error"/>, returns
        /// <c>null</c> or <c>default</c> depending on its type.
        /// </summary>
        public TValue Value { get; }

        /// <summary>
        /// Gets an error If <see cref="Kind"/> is <see cref="ResultKind.Error"/>;
        /// otherwise <c>null</c>.
        /// </summary>
        public Exception? Error { get; }

        /// <inheritdoc />
        public bool Equals(Result<TValue> other)
            => Error == other.Error &&
               Equals(Value, other.Value);

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is Result<TValue> result && Equals(result);
        }

        /// <inheritdoc />
        public override int GetHashCode()
            => Error is not null
                ? Error.GetHashCode()
                : Value?.GetHashCode() ?? 0;

        /// <summary>
        /// Creates a new error result.
        /// </summary>
        /// <param name="error">An arbitrary error.</param>
        /// <returns>An error result.</returns>
        public static Result<TValue> Reject(Exception error) => new(error);

        /// <summary>
        /// Creates a new value result.
        /// </summary>
        /// <param name="value">An arbitrary value.</param>
        /// <returns>A value result.</returns>
        public static Result<TValue> Resolve(TValue value) => new(value);

        /// <summary>
        /// Creates a new error result or a null result.
        /// </summary>
        /// <param name="error">An arbitrary error.</param>
        public static implicit operator Result<TValue>(Exception? error)
            => error is null ? new Result<TValue>(default(TValue)!) : new Result<TValue>(error);

        /// <summary>
        /// Creates a new value result.
        /// </summary>
        /// <param name="value">An arbitrary value.</param>
        public static implicit operator Result<TValue>(TValue value)
            => new(value);

        /// <summary>
        /// Extracts the error from a result.
        /// </summary>
        /// <param name="result">An arbitrary result.</param>
        public static implicit operator Exception?(Result<TValue> result)
            => result.Error;

        /// <summary>
        /// Extracts the value from a result.
        /// </summary>
        /// <param name="result">An arbitrary result.</param>
        public static implicit operator TValue(Result<TValue> result)
            => result.Value;
    }
}
