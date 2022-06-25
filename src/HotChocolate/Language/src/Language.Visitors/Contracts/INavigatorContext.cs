namespace HotChocolate.Language.Visitors;

/// <summary>
/// A visitor context that contains a syntax navigator.
/// </summary>
public interface INavigatorContext : ISyntaxVisitorContext
{
    /// <summary>
    /// Gets the associated <see cref="ISyntaxNavigator" /> from the current context.
    /// </summary>
    ISyntaxNavigator Navigator { get; }
}

/// <summary>
/// A base implementation of the visitor context that contains a syntax navigator.
/// </summary>
public class NavigatorContext : INavigatorContext
{
    /// <inheritdoc cref="INavigatorContext.Navigator"/>
    public ISyntaxNavigator Navigator { get; } = new DefaultSyntaxNavigator();
}
