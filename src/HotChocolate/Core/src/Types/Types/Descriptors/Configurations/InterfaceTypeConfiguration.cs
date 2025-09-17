using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors.Configurations;

public class InterfaceTypeConfiguration : TypeConfiguration, IComplexOutputTypeConfiguration
{
    private List<Type>? _knownClrTypes;
    private List<TypeReference>? _interfaces;

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeConfiguration"/>.
    /// </summary>
    public InterfaceTypeConfiguration() { }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeConfiguration"/>.
    /// </summary>
    public InterfaceTypeConfiguration(
        string name,
        string? description = null,
        Type? runtimeType = null)
        : base(runtimeType ?? typeof(object))
    {
        Name = name.EnsureGraphQLName();
        Description = description;
    }

    public IList<Type> KnownRuntimeTypes => _knownClrTypes ??= [];

    public ResolveAbstractType? ResolveAbstractType { get; set; }

    public IList<TypeReference> Interfaces => _interfaces ??= [];

    /// <summary>
    /// Specifies if this definition has interfaces.
    /// </summary>
    public bool HasInterfaces => _interfaces is { Count: > 0 };

    public IBindableList<InterfaceFieldConfiguration> Fields { get; } =
        new BindableList<InterfaceFieldConfiguration>();

    public override IEnumerable<ITypeSystemConfigurationTask> GetTasks()
    {
        List<ITypeSystemConfigurationTask>? configs = null;

        if (HasTasks)
        {
            configs ??= [];
            configs.AddRange(Tasks);
        }

        foreach (var field in Fields)
        {
            if (field.HasTasks)
            {
                configs ??= [];
                configs.AddRange(field.Tasks);
            }

            if (field.HasArguments)
            {
                foreach (var argument in field.Arguments)
                {
                    if (argument.HasTasks)
                    {
                        configs ??= [];
                        configs.AddRange(argument.Tasks);
                    }
                }
            }
        }

        return configs ?? Enumerable.Empty<ITypeSystemConfigurationTask>();
    }

    internal IReadOnlyList<Type> GetKnownClrTypes()
    {
        if (_knownClrTypes is null)
        {
            return [];
        }

        return _knownClrTypes;
    }

    internal IReadOnlyList<TypeReference> GetInterfaces()
    {
        if (_interfaces is null)
        {
            return [];
        }

        return _interfaces;
    }
}
