namespace HotChocolate.Types;

public interface IReadOnlyDirectiveCollection : IReadOnlyList<IDirective>
{
    /// <summary>
    /// Gets a directive by its name.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive.
    /// </param>
    IEnumerable<IDirective> this[string directiveName] { get; }

    /// <summary>
    /// Gets the first directive that matches the specified name.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive.
    /// </param>
    /// <returns>
    /// The first directive that matches the specified name.
    /// </returns>
    IDirective? FirstOrDefault(string directiveName);

    /// <summary>
    /// Determines whether the collection contains a directive with the specified name.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the collection contains a directive with the specified name;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool ContainsName(string directiveName);
}
