using HotChocolate.Configuration;

namespace HotChocolate.Types.Descriptors
{
    public interface IDescriptorContext
    {
        IReadOnlySchemaOptions Options { get; }

        INamingConventions Naming { get; }

        ITypeInspector Inspector { get; }

        T GetConventionOrDefault<T>(T defaultConvention)
            where T : class, IConvention;
    }
}
