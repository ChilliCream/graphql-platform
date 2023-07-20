using System;

namespace HotChocolate.Language.Utilities;

/// <summary>
/// Defines the indentation options for the <see cref="SyntaxSerializer" />.
/// </summary>
public sealed class IndentationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IndentationOptions" /> class.
    /// </summary>
    /// <param name="directives">
    /// Defines the indentation options for directives.
    /// </param>
    public IndentationOptions(DirectiveIndentationOptions directives)
    {
        Directives = directives ?? throw new ArgumentNullException(nameof(directives));
    }
    
    /// <summary>
    /// Gets the indentation options for directives.
    /// </summary>
    public DirectiveIndentationOptions Directives { get; }

    /// <summary>
    /// Gets the default indentation options.
    /// </summary>
    public static IndentationOptions Default { get; } = new(new(int.MaxValue));
    
    /// <summary>
    /// Gets the indentation options that does not indent.
    /// </summary>
    public static IndentationOptions? None { get; } = null;
}