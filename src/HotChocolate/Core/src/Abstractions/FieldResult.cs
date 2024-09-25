using System.Diagnostics.CodeAnalysis;
using static HotChocolate.Properties.AbstractionResources;

namespace HotChocolate;

/// <summary>
/// Represents a mutation result.
/// </summary>
/// <typeparam name="TResult">
/// The success result type.
/// </typeparam>
public readonly struct FieldResult<TResult> : IFieldResult
{
    /// <summary>
    /// Initializes a mutation success result.
    /// </summary>
    /// <param name="value">
    /// The success result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public FieldResult(TResult value)
    {
        Value = value;
        Errors = null;
        IsSuccess = true;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult(object error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<object> errors)
    {
        if (errors == null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var temp = errors.ToArray();

        if (temp.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        foreach (var error in temp)
        {
            if (ReferenceEquals(null, error))
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }
        }

        Value = default;
        Errors = temp;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params object[] errors)
    {
        Value = default;
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        IsSuccess = false;
        IsError = !IsSuccess;
        IsError = !IsSuccess;

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        foreach (var error in errors)
        {
            if (ReferenceEquals(null, error))
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }
        }
    }

    /// <summary>
    /// Gets the success result value.
    /// If <see cref="IsSuccess"/> is <c>false</c> then this property will return <c>null</c>.
    /// </summary>
    public TResult? Value { get; }

    /// <summary>
    /// Gets the errors of this result.
    /// If <see cref="IsSuccess"/> is <c>true</c> then this property will return <c>null</c>.
    /// </summary>
    public IReadOnlyList<object>? Errors { get; }

    /// <summary>
    /// Defines if this mutation result represents a success result.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Defines if the mutation had an error and if the result represents a error result.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Errors))]
    public bool IsError { get; }

    object? IFieldResult.Value => IsSuccess ? Value : Errors;

    /// <summary>
    /// Implicitly converts the success result value <typeparamref name="TResult"/>
    /// to a mutation success result.
    /// </summary>
    /// <param name="result">
    /// The success result value.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Value"/>
    /// set to <paramref name="result"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult>(TResult result)
        => new(result);
}

/// <summary>
/// Represents a mutation result.
/// </summary>
/// <typeparam name="TResult">
/// The success result type.
/// </typeparam>
/// <typeparam name="TError">
/// The error type.
/// </typeparam>
public readonly struct FieldResult<TResult, TError> : IFieldResult
{
    /// <summary>
    /// Initializes a mutation success result.
    /// </summary>
    /// <param name="value">
    /// The success result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public FieldResult(TResult value)
    {
        Value = value;
        Errors = null;
        IsSuccess = true;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Gets the success result value.
    /// If <see cref="IsSuccess"/> is <c>false</c> then this property will return <c>null</c>.
    /// </summary>
    public TResult? Value { get; }

    /// <summary>
    /// Gets the errors of this result.
    /// If <see cref="IsSuccess"/> is <c>true</c> then this property will return <c>null</c>.
    /// </summary>
    public IReadOnlyList<object>? Errors { get; }

    /// <summary>
    /// Defines if this mutation result represents a success result.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Defines if the mutation had an error and if the result represents a error result.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Errors))]
    public bool IsError { get; }

    object? IFieldResult.Value => IsSuccess ? Value : Errors;

    /// <summary>
    /// Implicitly converts the success result value <typeparamref name="TResult"/>
    /// to a mutation success result.
    /// </summary>
    /// <param name="result">
    /// The success result value.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Value"/>
    /// set to <paramref name="result"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError>(TResult result)
        => new(result);

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError>([DisallowNull] TError error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError>(error);
    }
}

/// <summary>
/// Represents a mutation result.
/// </summary>
/// <typeparam name="TResult">
/// The success result type.
/// </typeparam>
/// <typeparam name="TError1">
/// The error type 1.
/// </typeparam>
/// <typeparam name="TError2">
/// The error type 2.
/// </typeparam>
public readonly struct FieldResult<TResult, TError1, TError2> : IFieldResult
{
    /// <summary>
    /// Initializes a mutation success result.
    /// </summary>
    /// <param name="value">
    /// The success result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public FieldResult(TResult value)
    {
        Value = value;
        Errors = null;
        IsSuccess = true;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError1 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError1> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError1[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError2 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError2> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError2[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<object> errors)
    {
        if (errors == null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var temp = errors.ToArray();

        if (temp.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        foreach (var error in temp)
        {
            if (ReferenceEquals(null, error))
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }
        }

        Value = default;
        Errors = temp;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params object[] errors)
    {
        Value = default;
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        IsSuccess = false;
        IsError = !IsSuccess;

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        foreach (var error in errors)
        {
            if (ReferenceEquals(null, error))
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }
        }
    }

    /// <summary>
    /// Gets the success result value.
    /// If <see cref="IsSuccess"/> is <c>false</c> then this property will return <c>null</c>.
    /// </summary>
    public TResult? Value { get; }

    /// <summary>
    /// Gets the errors of this result.
    /// If <see cref="IsSuccess"/> is <c>true</c> then this property will return <c>null</c>.
    /// </summary>
    public IReadOnlyList<object>? Errors { get; }

    /// <summary>
    /// Defines if this mutation result represents a success result.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Defines if the mutation had an error and if the result represents a error result.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Errors))]
    public bool IsError { get; }

    object? IFieldResult.Value => IsSuccess ? Value : Errors;

    /// <summary>
    /// Implicitly converts the success result value <typeparamref name="TResult"/>
    /// to a mutation success result.
    /// </summary>
    /// <param name="result">
    /// The success result value.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Value"/>
    /// set to <paramref name="result"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2>(TResult result)
        => new(result);

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError1"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2>(
        [DisallowNull] TError1 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2>(error);
    }

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError2"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2>(
        [DisallowNull] TError2 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2>(error);
    }
}

/// <summary>
/// Represents a mutation result.
/// </summary>
/// <typeparam name="TResult">
/// The success result type.
/// </typeparam>
/// <typeparam name="TError1">
/// The error type 1.
/// </typeparam>
/// <typeparam name="TError2">
/// The error type 2.
/// </typeparam>
/// <typeparam name="TError3">
/// The error type 3.
/// </typeparam>
public readonly struct FieldResult<TResult, TError1, TError2, TError3> : IFieldResult
{
    /// <summary>
    /// Initializes a mutation success result.
    /// </summary>
    /// <param name="value">
    /// The success result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public FieldResult(TResult value)
    {
        Value = value;
        Errors = null;
        IsSuccess = true;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError1 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError1> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError1[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError2 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError2> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError2[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError3 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError3> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError3[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<object> errors)
    {
        if (errors == null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var temp = errors.ToArray();

        if (temp.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        foreach (var error in temp)
        {
            if (ReferenceEquals(null, error))
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }
        }

        Value = default;
        Errors = temp;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params object[] errors)
    {
        Value = default;
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        IsSuccess = false;
        IsError = !IsSuccess;

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        foreach (var error in errors)
        {
            if (ReferenceEquals(null, error))
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }
        }
    }

    /// <summary>
    /// Gets the success result value.
    /// If <see cref="IsSuccess"/> is <c>false</c> then this property will return <c>null</c>.
    /// </summary>
    public TResult? Value { get; }

    /// <summary>
    /// Gets the errors of this result.
    /// If <see cref="IsSuccess"/> is <c>true</c> then this property will return <c>null</c>.
    /// </summary>
    public IReadOnlyList<object>? Errors { get; }

    /// <summary>
    /// Defines if this mutation result represents a success result.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Defines if the mutation had an error and if the result represents a error result.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Errors))]
    public bool IsError { get; }

    object? IFieldResult.Value => IsSuccess ? Value : Errors;

    /// <summary>
    /// Implicitly converts the success result value <typeparamref name="TResult"/>
    /// to a mutation success result.
    /// </summary>
    /// <param name="result">
    /// The success result value.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Value"/>
    /// set to <paramref name="result"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3>(TResult result)
        => new(result);

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError1"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3>(
        [DisallowNull] TError1 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2, TError3>(error);
    }

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError2"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3>(
        [DisallowNull] TError2 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2, TError3>(error);
    }

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError2"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3>(
        [DisallowNull] TError3 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2, TError3>(error);
    }
}

/// <summary>
/// Represents a mutation result.
/// </summary>
/// <typeparam name="TResult">
/// The success result type.
/// </typeparam>
/// <typeparam name="TError1">
/// The error type 1.
/// </typeparam>
/// <typeparam name="TError2">
/// The error type 2.
/// </typeparam>
/// <typeparam name="TError3">
/// The error type 3.
/// </typeparam>
/// <typeparam name="TError4">
/// The error type 4.
/// </typeparam>
public readonly struct FieldResult<TResult, TError1, TError2, TError3, TError4> : IFieldResult
{
    /// <summary>
    /// Initializes a mutation success result.
    /// </summary>
    /// <param name="value">
    /// The success result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public FieldResult(TResult value)
    {
        Value = value;
        Errors = null;
        IsSuccess = true;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError1 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError1> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError1[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError2 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError2> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError2[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError3 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError3> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError3[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError4 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError4> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError4[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<object> errors)
    {
        if (errors == null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var temp = errors.ToArray();

        if (temp.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        foreach (var error in temp)
        {
            if (ReferenceEquals(null, error))
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }
        }

        Value = default;
        Errors = temp;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params object[] errors)
    {
        Value = default;
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        IsSuccess = false;
        IsError = !IsSuccess;

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        foreach (var error in errors)
        {
            if (ReferenceEquals(null, error))
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }
        }
    }

    /// <summary>
    /// Gets the success result value.
    /// If <see cref="IsSuccess"/> is <c>false</c> then this property will return <c>null</c>.
    /// </summary>
    public TResult? Value { get; }

    /// <summary>
    /// Gets the errors of this result.
    /// If <see cref="IsSuccess"/> is <c>true</c> then this property will return <c>null</c>.
    /// </summary>
    public IReadOnlyList<object>? Errors { get; }

    /// <summary>
    /// Defines if this mutation result represents a success result.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Defines if the mutation had an error and if the result represents a error result.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Errors))]
    public bool IsError { get; }

    object? IFieldResult.Value => IsSuccess ? Value : Errors;

    /// <summary>
    /// Implicitly converts the success result value <typeparamref name="TResult"/>
    /// to a mutation success result.
    /// </summary>
    /// <param name="result">
    /// The success result value.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Value"/>
    /// set to <paramref name="result"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3, TError4>(TResult result)
        => new(result);

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError1"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3, TError4>(
        [DisallowNull] TError1 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2, TError3, TError4>(error);
    }

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError2"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3, TError4>(
        [DisallowNull] TError2 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2, TError3, TError4>(error);
    }

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError2"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3, TError4>(
        [DisallowNull] TError3 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2, TError3, TError4>(error);
    }

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError2"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3, TError4>(
        [DisallowNull] TError4 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2, TError3, TError4>(error);
    }
}

/// <summary>
/// Represents a mutation result.
/// </summary>
/// <typeparam name="TResult">
/// The success result type.
/// </typeparam>
/// <typeparam name="TError1">
/// The error type 1.
/// </typeparam>
/// <typeparam name="TError2">
/// The error type 2.
/// </typeparam>
/// <typeparam name="TError3">
/// The error type 3.
/// </typeparam>
/// <typeparam name="TError4">
/// The error type 4.
/// </typeparam>
/// <typeparam name="TError5">
/// The error type 5.
/// </typeparam>
public readonly struct FieldResult<TResult, TError1, TError2, TError3, TError4, TError5> : IFieldResult
{
    /// <summary>
    /// Initializes a mutation success result.
    /// </summary>
    /// <param name="value">
    /// The success result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public FieldResult(TResult value)
    {
        Value = value;
        Errors = null;
        IsSuccess = true;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError1 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError1> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError1[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError2 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError2> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError2[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError3 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError3> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError3[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError4 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError4> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError4[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

        /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error result value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public FieldResult([DisallowNull] TError5 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Value = default;
        Errors = new object[] { error, };
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<TError5> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var list = new List<object>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        Value = default;
        Errors = list;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params TError5[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }

            array[i] = error;
        }

        Value = default;
        Errors = array;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(IEnumerable<object> errors)
    {
        if (errors == null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var temp = errors.ToArray();

        if (temp.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        foreach (var error in temp)
        {
            if (ReferenceEquals(null, error))
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }
        }

        Value = default;
        Errors = temp;
        IsSuccess = false;
        IsError = !IsSuccess;
    }

    /// <summary>
    /// Initializes a mutation error result.
    /// </summary>
    /// <param name="errors">
    /// The error result values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - One of the error objects in the <paramref name="errors"/> collection is <c>null</c>.
    /// - <paramref name="errors"/> is empty.
    /// </exception>
    public FieldResult(params object[] errors)
    {
        Value = default;
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        IsSuccess = false;
        IsError = !IsSuccess;

        if (errors.Length == 0)
        {
            throw new ArgumentException(MutationResult_ErrorsIsEmpty, nameof(errors));
        }

        foreach (var error in errors)
        {
            if (ReferenceEquals(null, error))
            {
                throw new ArgumentException(MutationResult_ErrorElementIsNull, nameof(errors));
            }
        }
    }

    /// <summary>
    /// Gets the success result value.
    /// If <see cref="IsSuccess"/> is <c>false</c> then this property will return <c>null</c>.
    /// </summary>
    public TResult? Value { get; }

    /// <summary>
    /// Gets the errors of this result.
    /// If <see cref="IsSuccess"/> is <c>true</c> then this property will return <c>null</c>.
    /// </summary>
    public IReadOnlyList<object>? Errors { get; }

    /// <summary>
    /// Defines if this mutation result represents a success result.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Defines if the mutation had an error and if the result represents a error result.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Errors))]
    public bool IsError { get; }

    object? IFieldResult.Value => IsSuccess ? Value : Errors;

    /// <summary>
    /// Implicitly converts the success result value <typeparamref name="TResult"/>
    /// to a mutation success result.
    /// </summary>
    /// <param name="result">
    /// The success result value.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Value"/>
    /// set to <paramref name="result"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3, TError4, TError5>(TResult result)
        => new(result);

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError1"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3, TError4, TError5>(
        [DisallowNull] TError1 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2, TError3, TError4, TError5>(error);
    }

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError2"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3, TError4, TError5>(
        [DisallowNull] TError2 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2, TError3, TError4, TError5>(error);
    }

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError2"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3, TError4, TError5>(
        [DisallowNull] TError3 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2, TError3, TError4, TError5>(error);
    }

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError2"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3, TError4, TError5>(
        [DisallowNull] TError4 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2, TError3, TError4, TError5>(error);
    }

    /// <summary>
    /// Implicitly converts the error object <typeparamref name="TError2"/>
    /// to a mutation error result.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FieldResult{TResult}"/> with <see cref="Errors"/>
    /// set to <paramref name="error"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public static implicit operator FieldResult<TResult, TError1, TError2, TError3, TError4, TError5>(
        [DisallowNull] TError5 error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new FieldResult<TResult, TError1, TError2, TError3, TError4, TError5>(error);
    }
}
