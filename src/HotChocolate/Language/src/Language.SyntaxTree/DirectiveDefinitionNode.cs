using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// A GraphQL schema describes directives which are used to annotate various parts of a
/// GraphQL document as an indicator that they should be evaluated differently
/// by a validator, executor, or client tool such as a code generator.
/// https://spec.graphql.org/October2021/#sec-Type-System.Directives
/// </summary>
public sealed class DirectiveDefinitionNode : ITypeSystemDefinitionNode, IHasName
{
    /// <summary>
    /// Initializes a new instance of <see cref="DirectiveDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name that this syntax node holds.
    /// </param>
    /// <param name="description">
    /// The description of the directive.
    /// </param>
    /// <param name="isRepeatable">
    /// Defines that the directive is repeatable and can be applied multiple times.
    /// </param>
    /// <param name="arguments">
    /// The arguments of the directive-
    /// </param>
    /// <param name="locations">
    /// The locations to which the directive can be annotated.
    /// </param>
    public DirectiveDefinitionNode(
        Location? location,
        NameNode name,
        StringValueNode? description,
        bool isRepeatable,
        IReadOnlyList<InputValueDefinitionNode> arguments,
        IReadOnlyList<NameNode> locations)
    {
        Location = location;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        IsRepeatable = isRepeatable;
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        Locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.DirectiveDefinition;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// Gets the name of the directive.
    /// </summary>
    public NameNode Name { get; }

    /// <summary>
    /// Gets the description of this directive.
    /// </summary>
    public StringValueNode? Description { get; }

    /// <summary>
    /// Defines that this directive is repeatable and can be applied multiple times.
    /// </summary>
    public bool IsRepeatable { get; }

    /// <summary>
    /// Gets the argument definitions of this directive.
    /// </summary>
    public IReadOnlyList<InputValueDefinitionNode> Arguments { get; }

    /// <summary>
    /// Gets the locations to which this directive can be annotated to.
    /// </summary>
    public IReadOnlyList<NameNode> Locations { get; }

    /// <inheritdoc cref="ISyntaxNode"/>
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Description is { })
        {
            yield return Description;
        }

        yield return Name;

        foreach (var argument in Arguments)
        {
            yield return argument;
        }

        foreach (var location in Locations)
        {
            yield return location;
        }
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString() => SyntaxPrinter.Print(this, true);

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <param name="indented">
    /// A value that indicates whether the GraphQL output should be formatted,
    /// which includes indenting nested GraphQL tokens, adding
    /// new lines, and adding white space between property names and values.
    /// </param>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Location" /> with <paramref name="location" />.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location" />.
    /// </returns>
    public DirectiveDefinitionNode WithLocation(Location? location)
        => new(location, Name, Description, IsRepeatable, Arguments, Locations);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Name" /> with <paramref name="name" />.
    /// </summary>
    /// <param name="name">
    /// The name that shall be used to replace the current name.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name" />.
    /// </returns>
    public DirectiveDefinitionNode WithName(NameNode name)
        => new(Location, name, Description, IsRepeatable, Arguments, Locations);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Description" /> with <paramref name="description" />.
    /// </summary>
    /// <param name="description">
    /// The description that shall be used to replace the current description.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="description" />.
    /// </returns>
    public DirectiveDefinitionNode WithDescription(StringValueNode? description)
        => new(Location, Name, description, IsRepeatable, Arguments, Locations);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="IsRepeatable" /> with <paramref name="repeatable" />.
    /// </summary>
    /// <param name="repeatable">
    /// The repeatable that shall be used to replace the current <see cref="IsRepeatable"/>.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="repeatable" />.
    /// </returns>
    public DirectiveDefinitionNode AsRepeatable(bool repeatable = true)
        => new(Location, Name, Description, repeatable, Arguments, Locations);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Arguments" /> with <paramref name="arguments" />.
    /// </summary>
    /// <param name="arguments">
    /// The arguments that shall be used to replace the current arguments.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="arguments" />.
    /// </returns>
    public DirectiveDefinitionNode WithArguments(IReadOnlyList<InputValueDefinitionNode> arguments)
        => new(Location, Name, Description, IsRepeatable, arguments, Locations);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Arguments" /> with <paramref name="locations" />.
    /// </summary>
    /// <param name="locations">
    /// The locations that shall be used to replace the current locations.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="locations" />.
    /// </returns>
    public DirectiveDefinitionNode WithLocations(IReadOnlyList<NameNode> locations)
        => new(Location, Name, Description, IsRepeatable, Arguments, locations);
}
