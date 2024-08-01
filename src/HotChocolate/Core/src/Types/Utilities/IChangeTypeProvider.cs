using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Utilities;

/// <summary>
/// A <see cref="IChangeTypeProvider" /> is used by the type converter to create new converter.
/// Each <see cref="IChangeTypeProvider" /> can provide one ore multiple value converters.
/// </summary>
public interface IChangeTypeProvider
{
    /// <summary>
    /// Tries to create a converter that can convert a value that is of the
    /// type <paramref name="source"/> to a value of the type <paramref name="target"/>.
    /// If this type provider can only handle parts of the conversion it can refer back to the
    /// root converter to ask other <see cref="IChangeTypeProvider"/> to provide the rest of
    /// the type conversion.
    /// </summary>
    /// <param name="source">
    /// The source type.
    /// </param>
    /// <param name="target">
    /// The target type.
    /// </param>
    /// <param name="root">
    /// The root change type provider that has access to
    /// all registered <see cref="IChangeTypeProvider"/>.
    /// </param>
    /// <param name="converter">
    /// The converter that was produced by this instance.
    /// </param>
    /// <returns>
    /// Returns a boolean indicating if this <see cref="IChangeTypeProvider"/> was able to
    /// create a type converter.
    /// </returns>
    bool TryCreateConverter(
        Type source,
        Type target,
        ChangeTypeProvider root,
        [NotNullWhen(true)] out ChangeType? converter);
}
