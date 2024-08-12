using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents the field collection of a type.
/// </summary>
/// <typeparam name="T">
/// The field type.
/// </typeparam>
public interface IFieldCollection<out T> : IReadOnlyCollection<T> where T : class, IField
{
    /// <summary>
    /// Gets a field by its name.
    /// </summary>
    T this[string fieldName] { get; }

    /// <summary>
    /// Gets a field by its index.
    /// </summary>
    T this[int index] { get; }

    /// <summary>
    /// Checks if a field with the specified
    /// <paramref name="fieldName"/> exists in this collection.
    /// </summary>
    /// <param name="fieldName">
    /// The name of a field.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if a field with the specified <paramref name="fieldName"/> exists;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool ContainsField(string fieldName);
}

/// <summary>
/// This helper class provides extensions to the <see cref="IFieldCollection{T}" /> interface
/// to allow for more efficiency when using the interface.
/// </summary>
public static class FieldCollectionExtensions
{
    /// <summary>
    /// Tries to get a field by its name from the field collection.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the field.
    /// </typeparam>
    public static bool TryGetField<T>(
        this IFieldCollection<T> collection,
        string fieldName,
        [NotNullWhen(true)] out T? field)
        where T : class, IField
    {
        // if we use the default implementation we will use the TryGetField method on there.
        if (collection is FieldCollection<T> fc)
        {
            return fc.TryGetField(fieldName, out field);
        }

        // in any other case we simulate the behavior which is not as efficient.
        if (collection.ContainsField(fieldName))
        {
            field = collection[fieldName];
            return true;
        }

        field = default;
        return false;
    }
}
