using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Properties;

namespace HotChocolate;

/// <summary>
/// An aggregate error allows to pass a collection of error in a single error object.
/// </summary>
public class AggregateError : Error
{
    /// <summary>
    /// Initializes a new instance of <see cref="AggregateError"/>.
    /// </summary>
    /// <param name="errors">
    /// The errors.
    /// </param>
    public AggregateError(IEnumerable<IError> errors)
        : base(AbstractionResources.AggregateError_Message)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        Errors = errors.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AggregateError"/>.
    /// </summary>
    /// <param name="errors">
    /// The errors.
    /// </param>
    public AggregateError(params IError[] errors)
        : base(AbstractionResources.AggregateError_Message)
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    /// <summary>
    /// Gets the actual errors.
    /// </summary>
    public IReadOnlyList<IError> Errors { get; }
}

public interface IMutationResult
{
    object Value { get; }

    bool IsSuccess { get; }
}

public readonly struct MutationResult<TResult> : IMutationResult
{
    public MutationResult([DisallowNull] TResult result)
    {
        Result = result ?? throw new ArgumentNullException(nameof(result));
        Errors = null;
        IsSuccess = true;
    }

    public MutationResult(object error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Result = default;
        Errors = new[] { error };
        IsSuccess = false;
    }

    public MutationResult(IEnumerable<object> errors)
    {
        if (errors == null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        Result = default;
        Errors = errors.ToArray();
        IsSuccess = false;
    }

    public MutationResult(params object[] errors)
    {
        Result = default;
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        IsSuccess = false;
    }

    public TResult? Result { get; }

    public IReadOnlyList<object>? Errors { get; }

#if NET5_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(Result))]
    [MemberNotNullWhen(false, nameof(Errors))]
#endif
    public bool IsSuccess { get; }

#if NET5_0_OR_GREATER
    object IMutationResult.Value => IsSuccess ? Result : Errors;
#else
    object IMutationResult.Value => IsSuccess ? Result! : Errors!;
#endif

    public static implicit operator MutationResult<TResult>([DisallowNull] TResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return new MutationResult<TResult>(result);
    }
}

public readonly struct MutationResult<TResult, TError> : IMutationResult
{
    public MutationResult([DisallowNull] TResult result)
    {
        Result = result ?? throw new ArgumentNullException(nameof(result));
        Errors = null;
        IsSuccess = true;
    }

    public MutationResult([DisallowNull] TError error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Result = default;
        Errors = new object[] { error };
        IsSuccess = false;
    }

    public MutationResult(IEnumerable<TError> errors)
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
                // TODO : message
                throw new ArgumentException("Errors are not allowed to be null", nameof(errors));
            }

            list.Add(error);
        }

        if (list.Count == 0)
        {
            // TODO : message
            throw new ArgumentException("The errors collection must at least specify one error.", nameof(errors));
        }

        Result = default;
        Errors = list;
        IsSuccess = false;
    }

    public MutationResult(params TError[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            // TODO : message
            throw new ArgumentException("The errors collection must at least specify one error.", nameof(errors));
        }

        var array = new object[errors.Length];

        for (var i = 0; i < errors.Length; i++)
        {
            var error = errors[i];

            if (error is null)
            {
                // TODO : message
                throw new ArgumentException("Errors are not allowed to be null", nameof(errors));
            }

            array[i] = error;
        }

        Result = default;
        Errors = array;
        IsSuccess = false;
    }

    public TResult? Result { get; }

    public IReadOnlyList<object>? Errors { get; }

#if NET5_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(Result))]
    [MemberNotNullWhen(false, nameof(Errors))]
#endif
    public bool IsSuccess { get; }

#if NET5_0_OR_GREATER
    object IMutationResult.Value => IsSuccess ? Result : Errors;
#else
    object IMutationResult.Value => IsSuccess ? Result! : Errors!;
#endif

    public static implicit operator MutationResult<TResult, TError>([DisallowNull] TResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return new MutationResult<TResult, TError>(result);
    }

    public static implicit operator MutationResult<TResult, TError>([DisallowNull] TError error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new MutationResult<TResult, TError>(error);
    }
}

public class Test
{
    public async Task<MutationResult<int, string>> Mutation()
    {
        return await Task.FromResult<string>("1");
    }
}
