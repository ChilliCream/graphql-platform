using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Skimmed.Utilities;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a collection of argument value assignments.
/// </summary>
public sealed class ArgumentAssignmentCollection(IReadOnlyList<ArgumentAssignment> arguments)
    : IReadOnlyList<ArgumentAssignment>
{
    private readonly IReadOnlyDictionary<string, ArgumentAssignment> _arguments =
        ToDictionary(arguments);

    /// <summary>
    /// Gets the number of argument assignments in the collection.
    /// </summary>
    /// <value>
    /// The number of argument assignments in the collection.
    /// </value>
    public int Count => _arguments.Count;

    /// <summary>
    /// Defines if this collection is read-only.
    /// </summary>
    /// <value>
    /// <c>true</c>, if this collection is read-only;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool IsReadOnly => true;

    /// <summary>
    /// Gets the argument assignment with the specified <paramref name="argumentName"/>.
    /// </summary>
    public IValueNode this[string argumentName] => _arguments[argumentName].Value;

    /// <summary>
    /// Gets the argument assignment at the specified <paramref name="index"/>.
    /// </summary>
    public ArgumentAssignment this[int index] => arguments[index];

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
    public bool TryGetValue(string argumentName, [NotNullWhen(true)] out IValueNode? value)
    {
        if (_arguments.TryGetValue(argumentName, out var arg))
        {
            value = arg.Value;
            return true;
        }

        value = null;
        return false;
    }

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
    public IValueNode? GetValueOrDefault(string argumentName, IValueNode? defaultValue = null)
        => _arguments.TryGetValue(argumentName, out var value)
            ? value.Value
            : defaultValue;

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
    public bool ContainsName(string argumentName)
        => _arguments.ContainsKey(argumentName);

    /// <summary>
    /// Checks if the collection contains the specified <paramref name="argument"/>.
    /// </summary>
    /// <param name="argument">
    /// The argument assignment.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the collection contains the specified <paramref name="argument"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(ArgumentAssignment argument)
        => arguments.Contains(argument);

    /// <summary>
    /// Copies the argument assignments to the specified <paramref name="array"/>.
    /// </summary>
    /// <param name="array">
    /// The array to which the argument assignments shall be copied.
    /// </param>
    /// <param name="arrayIndex">
    /// The index in the <paramref name="array"/> at which the copying shall start.
    /// </param>
    public void CopyTo(ArgumentAssignment[] array, int arrayIndex)
    {
        foreach (var argument in arguments)
        {
            array[arrayIndex++] = argument;
        }
    }

    /// <inheritdoc />
    public IEnumerator<ArgumentAssignment> GetEnumerator()
        => arguments.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    private static IReadOnlyDictionary<string, ArgumentAssignment> ToDictionary(
        IReadOnlyList<ArgumentAssignment> arguments)
        => arguments.Count switch
            {
                1 => new OneItemDictionary<string, ArgumentAssignment>(
                        arguments[0].Name,
                        arguments[0]),
                2 => new TwoItemDictionary<string, ArgumentAssignment>(
                        arguments[0].Name,
                        arguments[0],
                        arguments[1].Name,
                        arguments[1]),
                _ => arguments.ToFrozenDictionary(
                        t => t.Name,
                        t => t,
                        StringComparer.Ordinal),
            };
}
