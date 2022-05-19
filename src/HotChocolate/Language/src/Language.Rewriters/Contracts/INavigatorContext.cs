namespace HotChocolate.Language.Rewriters;

/// <summary>
/// A rewriter context that contains a syntax navigator.
/// </summary>
public interface INavigatorContext
{
    /// <summary>
    /// Gets the associated <see cref="ISyntaxNavigator" /> from the current context.
    /// </summary>
    ISyntaxNavigator Navigator { get; }
}
