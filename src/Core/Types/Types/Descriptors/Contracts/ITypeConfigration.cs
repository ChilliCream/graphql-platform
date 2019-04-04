using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    internal interface ITypeConfigration
    {
        ConfigurationKind Kind { get; }
        void Configure(DefinitionBase definition);
    }
}
