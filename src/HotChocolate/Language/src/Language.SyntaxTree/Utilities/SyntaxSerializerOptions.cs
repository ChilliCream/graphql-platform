using System;

namespace HotChocolate.Language.Utilities;

public struct SyntaxSerializerOptions
{
    /// <summary>
    /// Gets or sets a value that indicates whether the <see cref="SyntaxSerializer" />
    /// should format the GraphQL output, which includes indenting nested GraphQL tokens, adding
    /// new lines, and adding white space between property names and values.
    /// </summary>
    /// <value>
    /// <c>true</c> to format the GraphQL output; <c>false</c> to write without any extra
    /// white space. The default is false.
    /// </value>
    [Obsolete("Use Indentation instead.")]
    public bool Indented
    {
        readonly get => Indentation is not null;
        set
        {
            if (value && Indentation is null)
            {
                Indentation = IndentationOptions.Default;
            }
            else if (!value)
            {
                Indentation = null;
            }
        } 
    }

    /// <summary>
    /// Gets or sets a value that defines the indentation options for the  <see cref="SyntaxSerializer" />.
    /// </summary>
    public IndentationOptions? Indentation { get; set; }
}