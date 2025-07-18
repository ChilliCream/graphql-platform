#nullable enable

using System.Text.RegularExpressions;
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
    DirectiveLocation.Object
    | DirectiveLocation.Interface
    | DirectiveLocation.Union
    | DirectiveLocation.InputObject
    | DirectiveLocation.Enum
    | DirectiveLocation.Scalar
    | DirectiveLocation.FieldDefinition
    | DirectiveLocation.InputFieldDefinition
    | DirectiveLocation.ArgumentDefinition
    | DirectiveLocation.EnumValue
    | DirectiveLocation.Schema,
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
public sealed partial class Tag
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
        ArgumentNullException.ThrowIfNull(name);

        if (!ValidNameRegex().IsMatch(name))
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

    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    private static partial Regex ValidNameRegex();
}
