using System.Globalization;
using System.Runtime.CompilerServices;

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

internal static class TypeExtensions
{
    public static bool IsInputType(this IType type)
        => type.Kind switch
        {
            TypeKind.Interface or TypeKind.Object or TypeKind.Union => false,
            TypeKind.InputObject or TypeKind.Enum or TypeKind.Scalar => true,
            TypeKind.List => IsInputType(((ListType)type).ElementType),
            TypeKind.NonNull => IsInputType(((NonNullType)type).InnerType),
            _ => throw new NotSupportedException(),
        };

    public static bool IsOutputType(this IType type)
        => type.Kind switch
        {
            TypeKind.Interface or TypeKind.Object or TypeKind.Union => true,
            TypeKind.InputObject or TypeKind.Enum or TypeKind.Scalar => false,
            TypeKind.List => IsOutputType(((ListType)type).ElementType),
            TypeKind.NonNull => IsOutputType(((NonNullType)type).InnerType),
            _ => throw new NotSupportedException(),
        };
}

internal static class ArgumentAssertExtensions
{
    public static T ExpectNotNull<T>(this T? value, [CallerArgumentExpression("value")] string name = "value") where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(name);
        }

        return value;
    }

    public static IType ExpectInputType(this IType type, [CallerArgumentExpression("type")] string name = "type")
    {
        if (type is null)
        {
            throw new ArgumentNullException(name);
        }

        if (!type.IsInputType())
        {
            throw new ArgumentException("Must be an input type.", name);
        }

        return type;
    }
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

    public IDictionary<string, object?> ContextData => new Dictionary<string, object?>();
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

    IType Type { get; set; }
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

public sealed class Argument : IField
{
    private IType _type;
    private string _name;

    public Argument(string name, IType? type = null)
    {
        _name = name.ExpectNotNull();
        _type = type ?? NotSetType.Default;
    }

    public string Name
    {
        get => _name;
        set => _name = value.ExpectNotNull();
    }

    public string? Description { get; set; }

    public IDirectiveCollection Directives => throw new NotImplementedException();

    public IDictionary<string, object?> ContextData => new Dictionary<string, object?>();

    public IType Type
    {
        get => _type;
        set => _type = value.ExpectInputType();
    }
}
