// StrawberryShake.CodeGeneration.CSharp.Generators.StoreAccessorGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class StarWarsClientStoreAccessor
        : global::StrawberryShake.IStoreAccessor
    {
        public StarWarsClientStoreAccessor(
            global::StrawberryShake.IOperationStore operationStore,
            global::StrawberryShake.IEntityStore entityStore)
        {
            EntityStore = entityStore;
            OperationStore = operationStore;
        }

        public global::StrawberryShake.IOperationStore OperationStore { get; }

        public global::StrawberryShake.IEntityStore EntityStore { get; }
    }
}
