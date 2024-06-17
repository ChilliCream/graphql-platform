namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a collection of directives.
/// </summary>
public interface IDirectiveCollection : ICollection<Directive>
{
    /// <summary>
    /// Gets a directive by its name.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive.
    /// </param>
    IEnumerable<Directive> this[string directiveName] { get; }

    /// <summary>
    /// Gets the first directive that matches the specified name.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive.
    /// </param>
    /// <returns>
    /// The first directive that matches the specified name.
    /// </returns>
    Directive? FirstOrDefault(string directiveName);

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

    /// <summary>
    /// Replaces a directive with a new directive.
    /// </summary>
    /// <param name="currentDirective">
    /// The directive to replace.
    /// </param>
    /// <param name="newDirective">
    /// The new directive.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the directive was replaced; otherwise, <c>false</c>.
    /// </returns>
    bool Replace(Directive currentDirective, Directive newDirective);
}
