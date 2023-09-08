#nullable enable

using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents all the selection set variants of a field.
/// </summary>
public interface ISelectionVariants
{
    /// <summary>
    /// Gets the operation unique id for this variant.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets all the possible return types of the field to which this variant belongs to. 
    /// </summary>
    IEnumerable<IObjectType> GetPossibleTypes();
    
    /// <summary>
    /// Evaluates if the specified type context is a possible type for this variant.
    /// </summary>
    /// <param name="typeContext">
    /// The type context to evaluate.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the specified type context is a possible type for this variant;
    /// </returns>
    bool IsPossibleType(IObjectType typeContext);

    /// <summary>
    /// Gets the selection set for the specified field return type.
    /// </summary>
    /// <param name="typeContext">
    /// The field return type.
    /// </param>
    /// <returns>
    /// Returns the selection set for the specified field return type.
    /// </returns>
    /// <exception cref="System.ArgumentException">
    /// Invalid field return type.
    /// </exception> 
    ISelectionSet GetSelectionSet(IObjectType typeContext);
}
