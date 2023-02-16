namespace HotChocolate.Skimmed;

public class Schema
{
    public TypeCollection Types { get; }
}

public interface IType
{
    /// <summary>
    /// Gets the type kind.
    /// </summary>
    TypeKind Kind { get; }
}

public interface INamedType : IType
{
    /// <summary>
    /// Gets the field name.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets the description of the field.
    /// </summary>
    string? Description { get; set; }


    IDirectiveCollection Directives { get; }

    IDictionary<string, object?> ContextData { get; }
}

public class ObjectType : INamedType
{
    public ObjectType(string name)
    {
        Name = name;
    }

    public TypeKind Kind => TypeKind.Object;

    public string Name { get; set; }

    public string? Description { get; set; }

    public IDirectiveCollection Directives => throw new NotImplementedException();

    public IDictionary<string, object?> ContextData => throw new NotImplementedException();


}


public interface IField
{
    /// <summary>
    /// Gets the field name.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets the description of the field.
    /// </summary>
    string? Description { get; set; }


    IDirectiveCollection Directives { get; }


    IDictionary<string, object?> ContextData { get; }
}

/// <summary>
/// Represents a collection of directives of a <see cref="ITypeSystemMember"/>.
/// </summary>
public interface IDirectiveCollection : ICollection<Directive>
{
    /// <summary>
    /// Gets all directives of a certain directive type.
    /// </summary>
    /// <param name="directiveName"></param>
    IEnumerable<Directive> this[string directiveName] { get; }

    /// <summary>
    /// Gets a directive by its index.
    /// </summary>
    Directive this[int index] { get; }

    /// <summary>
    /// Gets the first directive that matches the given name or <c>null</c>.
    /// </summary>
    /// <param name="directiveName">
    /// The directive name.
    /// </param>
    /// <returns>
    /// Returns the first directive that matches the given name or <c>null</c>.
    /// </returns>
    Directive? FirstOrDefault(string directiveName);

    /// <summary>
    /// Checks if a directive with the specified <paramref name="directiveName"/> exists.
    /// </summary>
    /// <param name="directiveName">
    /// The directive name.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if a directive with the specified <paramref name="directiveName"/>
    /// exists; otherwise, <c>false</c> will be returned.
    /// </returns>
    bool ContainsDirective(string directiveName);
}

public sealed class Directive
{
    public Directive(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the field name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the description of the field.
    /// </summary>
    public string? Description { get; set; }
}

public class Argument : IField
{
    public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string? Description { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public IDirectiveCollection Directives => throw new NotImplementedException();

    public IDictionary<string, object?> ContextData => throw new NotImplementedException();
}
