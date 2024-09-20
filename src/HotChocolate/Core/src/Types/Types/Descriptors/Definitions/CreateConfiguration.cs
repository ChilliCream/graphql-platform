#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public sealed class CreateConfiguration : ITypeSystemMemberConfiguration
{
    private readonly Action<IDescriptorContext, IDefinition> _configure;

    public CreateConfiguration(
        Action<IDescriptorContext, IDefinition> configure,
        IDefinition owner)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public IDefinition Owner { get; }

    public ApplyConfigurationOn On => ApplyConfigurationOn.Create;

    public IReadOnlyList<TypeDependency> Dependencies { get; } = Array.Empty<TypeDependency>();

    public void AddDependency(TypeDependency dependency)
        => throw new NotSupportedException(
            "Create configurations do not support dependencies.");

    public void Configure(IDescriptorContext context)
        => _configure(context, Owner);

    public ITypeSystemMemberConfiguration Copy(DefinitionBase newOwner)
    {
        if (newOwner is null)
        {
            throw new ArgumentNullException(nameof(newOwner));
        }

        return new CreateConfiguration(_configure, newOwner);
    }
}
