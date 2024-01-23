using System;
using System.Collections.Generic;
using static HotChocolate.Properties.AbstractionResources;

namespace HotChocolate;

/// <summary>
/// This class represents a mutation error and is mean to be used within mutation convention extensions.
/// </summary>
public sealed class MutationError : IMutationResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="MutationError"/>.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    public MutationError(object error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Errors = new[] { error, };
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MutationError"/>.
    /// </summary>
    /// <param name="errors">
    /// The error objects.
    /// </param>
    public MutationError(IReadOnlyList<object> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Count == 0)
        {
            throw new ArgumentException(MutationError_ErrorsEmpty, nameof(errors));
        }

        Errors = errors;
    }

    /// <summary>
    /// Gets the error objects.
    /// </summary>
    public IReadOnlyList<object> Errors { get; }
    
    /// <summary>
    /// Gets the mutation result value.
    /// </summary>
    object IMutationResult.Value => Errors;

    /// <summary>
    /// Defines if the mutation was successful and if the result represents a success result.
    /// </summary>
    public bool IsSuccess => false;

    /// <summary>
    /// Defines if the mutation had an error and if the result represents a error result.
    /// </summary>
    public bool IsError => true;
}