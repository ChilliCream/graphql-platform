namespace HotChocolate.Language.Rewriters;

public interface INavigatorContext
{
    /// <summary>
    /// Gets the associated <see cref="ISyntaxNavigator" /> from the current context.
    /// </summary>
    ISyntaxNavigator Navigator { get; }
}
