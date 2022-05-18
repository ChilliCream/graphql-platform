namespace HotChocolate.Language.Rewriters;

public interface IHasNavigator
{
    ISyntaxNavigator Navigator { get; }
}
