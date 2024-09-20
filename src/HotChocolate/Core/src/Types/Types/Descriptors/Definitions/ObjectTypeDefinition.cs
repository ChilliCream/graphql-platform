using System.Collections.Immutable;
using System.Reflection;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Defines the properties of a GraphQL object type.
/// </summary>
public class ObjectTypeDefinition
    : TypeDefinitionBase
    , IComplexOutputTypeDefinition
{
    private List<Type>? _knownClrTypes;
    private List<TypeReference>? _interfaces;
    private List<ObjectFieldBinding>? _fieldIgnores;
    private FieldBindingFlags _fieldBindingFlags = FieldBindingFlags.Instance;

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
    /// </summary>
    public ObjectTypeDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
    /// </summary>
    public ObjectTypeDefinition(
        string name,
        string? description = null,
        Type? runtimeType = null)
        : base(runtimeType ?? typeof(object))
    {
        Name = name;
        Description = description;
        FieldBindingType = runtimeType;
    }

    /// <summary>
    /// Gets or sets the .net type representation of this type.
    /// </summary>
    public override Type RuntimeType
    {
        get => base.RuntimeType;
        set
        {
            base.RuntimeType = value;
            FieldBindingType = value;
        }
    }

    /// <summary>
    /// The type that shall be used to infer fields from.
    /// </summary>
    public Type? FieldBindingType { get; set; }

    /// <summary>
    /// Gets the type that can provide attributes to this type.
    /// </summary>
    public ImmutableArray<Type> AttributeBindingTypes { get; set; } = ImmutableArray<Type>.Empty;

    /// <summary>
    /// Runtime types that also represent this GraphQL type.
    /// </summary>
    public IList<Type> KnownRuntimeTypes =>
        _knownClrTypes ??= [];

    /// <summary>
    /// Gets fields that shall be ignored.
    /// </summary>
    public IList<ObjectFieldBinding> FieldIgnores =>
        _fieldIgnores ??= [];

    /// <summary>
    /// A delegate to determine if a resolver result is of this object type.
    /// </summary>
    public IsOfType? IsOfType { get; set; }

    /// <summary>
    /// Defines if this type definition represents a object type extension.
    /// </summary>
    public bool IsExtension { get; set; }

    /// <summary>
    /// Gets the interfaces that this object type implements.
    /// </summary>
    public IList<TypeReference> Interfaces =>
        _interfaces ??= [];

    /// <summary>
    /// Specifies if this definition has interfaces.
    /// </summary>
    public bool HasInterfaces => _interfaces is { Count: > 0, };

    /// <summary>
    /// Gets the fields of this object type.
    /// </summary>
    public IBindableList<ObjectFieldDefinition> Fields { get; } =
        new BindableList<ObjectFieldDefinition>();

    /// <summary>
    /// Gets the field binding flags, which defines how runtime
    /// members are inferred as GraphQL fields.
    /// </summary>
    public FieldBindingFlags FieldBindingFlags
    {
        get => Fields.BindingBehavior is BindingBehavior.Explicit
            ? FieldBindingFlags.Default
            : _fieldBindingFlags;
        set
        {
            Fields.BindingBehavior =
                value == FieldBindingFlags.Default
                    ? BindingBehavior.Explicit
                    : BindingBehavior.Implicit;
            _fieldBindingFlags = value;
        }
    }

    public override IEnumerable<ITypeSystemMemberConfiguration> GetConfigurations()
    {
        List<ITypeSystemMemberConfiguration>? configs = null;

        if (HasConfigurations)
        {
            configs ??= [];
            configs.AddRange(Configurations);
        }

        foreach (var field in Fields)
        {
            if (field.HasConfigurations)
            {
                configs ??= [];
                configs.AddRange(field.Configurations);
            }

            foreach (var argument in field.GetArguments())
            {
                if (argument.HasConfigurations)
                {
                    configs ??= [];
                    configs.AddRange(argument.Configurations);
                }
            }
        }

        return configs ?? Enumerable.Empty<ITypeSystemMemberConfiguration>();
    }

    internal IReadOnlyList<Type> GetKnownClrTypes()
    {
        if (_knownClrTypes is null)
        {
            return Array.Empty<Type>();
        }

        return _knownClrTypes;
    }

    internal IReadOnlyList<TypeReference> GetInterfaces()
    {
        if (_interfaces is null)
        {
            return Array.Empty<TypeReference>();
        }

        return _interfaces;
    }

    internal IReadOnlyList<ObjectFieldBinding> GetFieldIgnores()
    {
        if (_fieldIgnores is null)
        {
            return Array.Empty<ObjectFieldBinding>();
        }

        return _fieldIgnores;
    }

    protected internal void CopyTo(ObjectTypeDefinition target)
    {
        base.CopyTo(target);

        if (_knownClrTypes is { Count: > 0, })
        {
            target._knownClrTypes = [.._knownClrTypes,];
        }

        if (_interfaces is { Count: > 0, })
        {
            target._interfaces = [.._interfaces,];
        }

        if (_fieldIgnores is { Count: > 0, })
        {
            target._fieldIgnores = [.._fieldIgnores,];
        }

        if (Fields is { Count: > 0, })
        {
            target.Fields.Clear();

            foreach (var field in Fields)
            {
                target.Fields.Add(field);
            }
        }

        if(AttributeBindingTypes.Length > 0)
        {
            target.AttributeBindingTypes = AttributeBindingTypes;
        }

        target.FieldBindingType = FieldBindingType;
        target.IsOfType = IsOfType;
        target.IsExtension = IsExtension;
    }

    protected internal void MergeInto(ObjectTypeDefinition target)
    {
        base.MergeInto(target);

        if (_knownClrTypes is { Count: > 0, })
        {
            target._knownClrTypes ??= [];
            target._knownClrTypes.AddRange(_knownClrTypes);
        }

        if (_interfaces is { Count: > 0, })
        {
            target._interfaces ??= [];
            target._interfaces.AddRange(_interfaces);
        }

        if (_fieldIgnores is { Count: > 0, })
        {
            target._fieldIgnores ??= [];
            target._fieldIgnores.AddRange(_fieldIgnores);
        }

        if(AttributeBindingTypes.Length > 0)
        {
            target.AttributeBindingTypes = target.AttributeBindingTypes.AddRange(AttributeBindingTypes);
        }

        foreach (var field in Fields)
        {
            var targetField = field switch
            {
                { BindToField: { Type: ObjectFieldBindingType.Property, } bindTo, } =>
                    target.Fields.FirstOrDefault(t => bindTo.Name.EqualsOrdinal(t.Member?.Name!)),
                { BindToField: { Type: ObjectFieldBindingType.Field, } bindTo, } =>
                    target.Fields.FirstOrDefault(t => bindTo.Name.EqualsOrdinal(t.Name)),
                _ => target.Fields.FirstOrDefault(t => field.Name.EqualsOrdinal(t.Name)),
            };

            var replaceField = field.BindToField?.Replace ?? false;
            var removeField = field.Ignore;

            // we skip fields that have an incompatible parent.
            if (field.Member is MethodInfo p &&
                p.GetParameters() is { Length: > 0, } parameters)
            {
                var parent = parameters.FirstOrDefault(
                    t => t.IsDefined(typeof(ParentAttribute), true));
                if (parent is not null &&
                    !parent.ParameterType.IsAssignableFrom(target.RuntimeType) &&
                    !target.RuntimeType.IsAssignableFrom(parent.ParameterType))
                {
                    continue;
                }
            }

            if (removeField)
            {
                if (targetField is not null)
                {
                    target.Fields.Remove(targetField);
                }
            }
            else if (targetField is null || replaceField)
            {
                if (targetField is not null)
                {
                    target.Fields.Remove(targetField);
                }

                var newField = new ObjectFieldDefinition();
                field.CopyTo(newField);
                newField.SourceType = target.RuntimeType;

                SetResolverMember(newField, targetField);
                target.Fields.Add(newField);
            }
            else
            {
                SetResolverMember(field, targetField);
                field.MergeInto(targetField);
            }
        }

        target.IsOfType ??= IsOfType;
    }

    private static void SetResolverMember(
        ObjectFieldDefinition sourceField,
        ObjectFieldDefinition? targetField)
    {
        // we prepare the field that is merged in to use the resolver member instead of member.
        // this will ensure that the original source type member is preserved after we have
        // merged the type extensions.

        if (sourceField.Member is not null && sourceField.ResolverMember is null)
        {
            sourceField.ResolverMember = sourceField.Member;
            sourceField.Member = targetField?.Member;
        }
    }
}
