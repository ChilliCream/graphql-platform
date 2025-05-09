#nullable enable

using System.Runtime.CompilerServices;
using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// The @tag directive is used to apply arbitrary string
/// metadata to a schema location. Custom tooling can use
/// this metadata during any step of the schema delivery flow,
/// including composition, static analysis, and documentation.
///
/// <code>
/// interface Book {
///   id: ID! @tag(name: "your-value")
///   title: String!
///   author: String!
/// }
/// </code>
/// </summary>
[DirectiveType(
    DirectiveNames.Tag.Name,
    DirectiveLocation.Object |
    DirectiveLocation.Interface |
    DirectiveLocation.Union |
    DirectiveLocation.InputObject |
    DirectiveLocation.Enum |
    DirectiveLocation.Scalar |
    DirectiveLocation.FieldDefinition |
    DirectiveLocation.InputFieldDefinition |
    DirectiveLocation.ArgumentDefinition |
    DirectiveLocation.EnumValue |
    DirectiveLocation.Schema,
    IsRepeatable = true)]
[TagDirectiveConfig]
[GraphQLDescription(
    """
    The @tag directive is used to apply arbitrary string
    metadata to a schema location. Custom tooling can use
    this metadata during any step of the schema delivery flow,
    including composition, static analysis, and documentation.

    interface Book {
      id: ID! @tag(name: "your-value")
      title: String!
      author: String!
    }
    """)]
public sealed class Tag
{
    /// <summary>
    /// Creates a new instance of <see cref="Tag"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the tag.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <c>null</c>.
    /// </exception>
    public Tag(string name)
    {
        if (!IsValidTagName(name))
        {
            throw new ArgumentException(
                TypeResources.TagDirective_Name_NotValid,
                nameof(name));
        }

        Name = name;
    }

    /// <summary>
    /// The name of the tag.
    /// </summary>
    [GraphQLName(DirectiveNames.Tag.Arguments.Name)]
    [GraphQLDescription("The name of the tag.")]
    public string Name { get; }

    private static bool IsValidTagName(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var span = name.AsSpan();

        if (IsLetterOrUnderscore(span[0]))
        {
            if (span.Length > 1)
            {
                for (var i = 1; i < span.Length; i++)
                {
                    if (!IsLetterOrDigitOrUnderscoreOrHyphen(span[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLetterOrDigitOrUnderscoreOrHyphen(char c)
    {
        if (c is > (char)96 and < (char)123 or > (char)64 and < (char)91)
        {
            return true;
        }

        if (c is > (char)47 and < (char)58)
        {
            return true;
        }

        if (c == '_' || c == '-')
        {
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLetterOrUnderscore(char c)
    {
        if (c is > (char)96 and < (char)123 or > (char)64 and < (char)91)
        {
            return true;
        }

        if (c == '_')
        {
            return true;
        }

        return false;
    }
}
