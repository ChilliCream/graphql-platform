using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class StoreAccessorGenerator : CodeGenerator<StoreAccessorDescriptor>
    {
        private const string _operationStore = "operationStore";
        private const string _entityStore = "entityStore";
        private const string _entityIdSerializer = "entityIdSerializer";
        private const string _requestFactories = "requestFactories";
        private const string _resultDataFactories = "resultDataFactories";

        protected override void Generate(
            CodeWriter writer,
            StoreAccessorDescriptor descriptor,
            out string fileName,
            out string? path)
        {
            fileName = descriptor.Name;
            path = State;

            const string entityStore = "EntityStore";
            const string operationStore = "OperationStore";

            ClassBuilder factory = ClassBuilder
                .New(fileName)
                .SetAccessModifier(AccessModifier.Public)
                .AddImplements(TypeNames.StoreAccessor);

            factory
                .AddConstructor()
                .SetTypeName(fileName)
                .SetPublic()
                .AddParameter(_operationStore, x => x.SetType(TypeNames.IOperationStore))
                .AddParameter(_entityStore, x => x.SetType(TypeNames.IEntityStore))
                .AddParameter(_entityIdSerializer, x => x.SetType(TypeNames.IEntityIdSerializer))
                .AddParameter(
                    _requestFactories,
                    x => x.SetType(
                        TypeNames.IEnumerable.WithGeneric(TypeNames.IOperationRequestFactory)))
                .AddParameter(
                    _resultDataFactories,
                    x => x.SetType(
                        TypeNames.IEnumerable.WithGeneric(TypeNames.IOperationResultDataFactory)))
                .AddBase(_operationStore)
                .AddBase(_entityStore)
                .AddBase(_entityIdSerializer)
                .AddBase(_requestFactories)
                .AddBase(_resultDataFactories);

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(factory)
                .Build(writer);
        }
    }
}
