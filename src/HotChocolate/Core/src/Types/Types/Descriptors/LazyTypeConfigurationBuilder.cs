using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public static class LazyTypeConfigurationBuilder
    {
        public static LazyTypeConfigurationBuilder<T> New<T>()
            where T : DefinitionBase =>
            new();
    }
}
