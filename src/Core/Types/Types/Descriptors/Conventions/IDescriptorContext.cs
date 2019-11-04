using HotChocolate.Configuration;

namespace HotChocolate.Types.Descriptors
{
    public interface IDescriptorContext
    {
        IReadOnlySchemaOptions Options { get; }

        INamingConventions Naming { get; }

        ITypeInspector Inspector { get; }

        T GetConvention<T>() where T : IConvention;

        bool TryGetConvention<T>(out T convention) where T : IConvention;
    }
}
