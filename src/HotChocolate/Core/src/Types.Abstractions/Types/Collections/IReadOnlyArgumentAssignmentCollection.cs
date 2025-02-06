using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Types;

public interface IReadOnlyArgumentAssignmentCollection : IReadOnlyList<ArgumentAssignment>
{
    /// <summary>
    /// Gets the argument assignment with the specified <paramref name="argumentName"/>.
    /// </summary>
    IValueNode this[string argumentName] { get; }

    /// <summary>
    /// Tries to get the argument assignment with the specified <paramref name="argumentName"/>.
    /// </summary>
    /// <param name="argumentName">
    /// The name of the argument.
    /// </param>
    /// <param name="value">
    ///  The argument assignment with the specified <paramref name="argumentName"/>.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the argument assignment with the specified <paramref name="argumentName"/>
    /// was found; otherwise, <c>false</c>.
    /// </returns>
    bool TryGetValue(string argumentName, [NotNullWhen(true)] out IValueNode? value);

    /// <summary>
    /// Gets the assigned argument value with the specified <paramref name="argumentName"/> or
    /// returns a <paramref name="defaultValue"/>.
    /// </summary>
    /// <param name="argumentName">
    /// The name of the argument.
    /// </param>
    /// <param name="defaultValue">
    /// The default value that shall be returned if no argument assignment is not found.
    /// </param>
    /// <returns>
    /// Returns the assigned argument value with the specified <paramref name="argumentName"/> or
    /// returns a <paramref name="defaultValue"/>.
    /// </returns>
    IValueNode? GetValueOrDefault(string argumentName, IValueNode? defaultValue = null);

    /// <summary>
    /// Checks if the collection contains an argument assignment with the specified
    /// <paramref name="argumentName"/>.
    /// </summary>
    /// <param name="argumentName">
    /// The name of the argument.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the collection contains an argument assignment with the specified
    /// <paramref name="argumentName"/>; otherwise, <c>false</c>.
    /// </returns>
    bool ContainsName(string argumentName);
}
