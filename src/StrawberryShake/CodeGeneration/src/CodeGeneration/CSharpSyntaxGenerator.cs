using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration;

public abstract class CSharpSyntaxGenerator<TDescriptor>
    : ICSharpSyntaxGenerator
    where TDescriptor : ICodeDescriptor
{
    public bool CanHandle(
        ICodeDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings) =>
        descriptor is TDescriptor d && CanHandle(d, settings);

    protected virtual bool CanHandle(
        TDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings) =>
        true;

    public CSharpSyntaxGeneratorResult Generate(
        ICodeDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        return Generate((TDescriptor)descriptor, settings);
    }

    protected abstract CSharpSyntaxGeneratorResult Generate(
        TDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings);

    protected static string State => nameof(State);
    protected static string Components => nameof(Components);
    protected static string DependencyInjection => nameof(DependencyInjection);
    protected static string Serialization => nameof(Serialization);
}
