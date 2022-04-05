using System;
using System.Collections.Generic;
using HotChocolate.Configuration;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public sealed class CompleteConfiguration<TDefinition>
    : CompleteConfiguration
    where TDefinition : IDefinition
{
    public CompleteConfiguration(
        Action<ITypeCompletionContext, TDefinition> configure,
        TDefinition owner,
        ApplyConfigurationOn on,
        ITypeReference? typeReference = null,
        TypeDependencyKind kind = TypeDependencyKind.Default)
        : base((c, d) => configure(c, (TDefinition)d), owner, on, typeReference, kind)
    {
    }

    public CompleteConfiguration(
        Action<ITypeCompletionContext, TDefinition> configure,
        TDefinition owner,
        ApplyConfigurationOn on,
        IEnumerable<TypeDependency> dependencies)
        : base((c, d) => configure(c, (TDefinition)d), owner, on, dependencies)
    {
    }
}

public class CompleteConfiguration : ITypeSystemMemberConfiguration
{
    private readonly Action<ITypeCompletionContext, IDefinition> _configure;
    private List<TypeDependency>? _dependencies;

    public CompleteConfiguration(
        Action<ITypeCompletionContext, IDefinition> configure,
        IDefinition owner,
        ApplyConfigurationOn on,
        ITypeReference? typeReference = null,
        TypeDependencyKind kind = TypeDependencyKind.Default)
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
            _dependencies = new List<TypeDependency>(1) { new(typeReference, kind) };
        }
    }

    public CompleteConfiguration(
        Action<ITypeCompletionContext, IDefinition> configure,
        IDefinition owner,
        ApplyConfigurationOn on,
        IEnumerable<TypeDependency> dependencies)
    {
        if (on is ApplyConfigurationOn.Create)
        {
            throw new ArgumentOutOfRangeException(nameof(on));
        }

        if (dependencies is null)
        {
            throw new ArgumentNullException(nameof(dependencies));
        }

        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        On = on;
        _dependencies = new(dependencies);
    }

    public IDefinition Owner { get; }

    public ApplyConfigurationOn On { get; }

    public IReadOnlyList<TypeDependency> Dependencies =>
        _dependencies ?? (IReadOnlyList<TypeDependency>)Array.Empty<TypeDependency>();

    public void AddDependency(TypeDependency dependency)
    {
        if (dependency is null)
        {
            throw new ArgumentNullException(nameof(dependency));
        }

        _dependencies ??= new List<TypeDependency>();
        _dependencies.Add(dependency);
    }

    public void Configure(ITypeCompletionContext context)
        => _configure(context, Owner);

    public ITypeSystemMemberConfiguration Copy(DefinitionBase newOwner)
    {
        if (newOwner is null)
        {
            throw new ArgumentNullException(nameof(newOwner));
        }

        return new CompleteConfiguration(_configure, newOwner, On, Dependencies);
    }
}
