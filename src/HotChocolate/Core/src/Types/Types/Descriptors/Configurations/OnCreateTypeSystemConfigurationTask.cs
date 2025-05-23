#nullable enable

namespace HotChocolate.Types.Descriptors.Configurations;

public sealed class OnCreateTypeSystemConfigurationTask : ITypeSystemConfigurationTask
{
    private readonly Action<IDescriptorContext, ITypeSystemConfiguration> _configure;

    public OnCreateTypeSystemConfigurationTask(
        Action<IDescriptorContext, ITypeSystemConfiguration> configure,
        ITypeSystemConfiguration owner)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public ITypeSystemConfiguration Owner { get; }

    public ApplyConfigurationOn On => ApplyConfigurationOn.Create;

    public IReadOnlyList<TypeDependency> Dependencies { get; } = [];

    public void AddDependency(TypeDependency dependency)
        => throw new NotSupportedException(
            "Create configurations do not support dependencies.");

    public void Configure(IDescriptorContext context)
        => _configure(context, Owner);

    public ITypeSystemConfigurationTask Copy(TypeSystemConfiguration newOwner)
    {
        if (newOwner is null)
        {
            throw new ArgumentNullException(nameof(newOwner));
        }

        return new OnCreateTypeSystemConfigurationTask(_configure, newOwner);
    }
}
