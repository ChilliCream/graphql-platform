using HotChocolate.Configuration;

#nullable enable

namespace HotChocolate.Types.Descriptors.Configurations;

public sealed class OnCompleteTypeSystemConfigurationTask<TDefinition>
    : OnCompleteTypeSystemConfigurationTask
    where TDefinition : ITypeSystemConfiguration
{
    public OnCompleteTypeSystemConfigurationTask(
        Action<ITypeCompletionContext, TDefinition> configure,
        TDefinition owner,
        ApplyConfigurationOn on,
        TypeReference? typeReference = null,
        TypeDependencyFulfilled fulfilled = TypeDependencyFulfilled.Default)
        : base((c, d) => configure(c, (TDefinition)d), owner, on, typeReference, fulfilled)
    {
    }

    public OnCompleteTypeSystemConfigurationTask(
        Action<ITypeCompletionContext, TDefinition> configure,
        TDefinition owner,
        ApplyConfigurationOn on,
        IEnumerable<TypeDependency> dependencies)
        : base((c, d) => configure(c, (TDefinition)d), owner, on, dependencies)
    {
    }
}

public class OnCompleteTypeSystemConfigurationTask : ITypeSystemConfigurationTask
{
    private readonly Action<ITypeCompletionContext, ITypeSystemConfiguration> _configure;
    private List<TypeDependency>? _dependencies;

    public OnCompleteTypeSystemConfigurationTask(
        Action<ITypeCompletionContext, ITypeSystemConfiguration> configure,
        ITypeSystemConfiguration owner,
        ApplyConfigurationOn on,
        TypeReference? typeReference = null,
        TypeDependencyFulfilled fulfilled = TypeDependencyFulfilled.Default)
    {
        if (on is ApplyConfigurationOn.Create)
        {
            throw new ArgumentOutOfRangeException(nameof(on));
        }

        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        On = on;

        if (typeReference is not null)
        {
            _dependencies = [new(typeReference, fulfilled)];
        }
    }

    public OnCompleteTypeSystemConfigurationTask(
        Action<ITypeCompletionContext, ITypeSystemConfiguration> configure,
        ITypeSystemConfiguration owner,
        ApplyConfigurationOn on,
        IEnumerable<TypeDependency> dependencies)
    {
        if (on is ApplyConfigurationOn.Create)
        {
            throw new ArgumentOutOfRangeException(nameof(on));
        }

        ArgumentNullException.ThrowIfNull(dependencies);

        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        On = on;
        _dependencies = [.. dependencies];
    }

    public ITypeSystemConfiguration Owner { get; }

    public ApplyConfigurationOn On { get; }

    public IReadOnlyList<TypeDependency> Dependencies =>
        _dependencies ?? (IReadOnlyList<TypeDependency>)[];

    public void AddDependency(TypeDependency dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        _dependencies ??= [];
        _dependencies.Add(dependency);
    }

    public void Configure(ITypeCompletionContext context)
        => _configure(context, Owner);

    public ITypeSystemConfigurationTask Copy(TypeSystemConfiguration newOwner)
    {
        ArgumentNullException.ThrowIfNull(newOwner);

        return new OnCompleteTypeSystemConfigurationTask(_configure, newOwner, On, Dependencies);
    }
}
