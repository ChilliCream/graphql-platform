using System.Diagnostics.CodeAnalysis;

namespace StrawberryShake;

/// <summary>
/// Represents a fragment and allows to access the fragments data.
/// </summary>
/// <typeparam name="TData">
/// The type of the fragment data.
/// </typeparam>
public interface IFragment<TData> where TData : class
{
    /// <summary>
    /// Tries to retrieve the fragment data.
    /// </summary>
    /// <param name="data">
    /// The fragment data.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the fragment is fulfilled and
    /// the data can be retrieved; otherwise, <c>false</c>.
    /// </returns>
    bool TryGetData([NotNullWhen(true)] out TData? data);
}
