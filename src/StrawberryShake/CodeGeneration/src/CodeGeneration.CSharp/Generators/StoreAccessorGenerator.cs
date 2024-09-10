using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class StoreAccessorGenerator : CodeGenerator<StoreAccessorDescriptor>
{
    private const string _operationStore = "operationStore";
    private const string _entityStore = "entityStore";
    private const string _entityIdSerializer = "entityIdSerializer";
    private const string _requestFactories = "requestFactories";
    private const string _resultDataFactories = "resultDataFactories";

    protected override bool CanHandle(
        StoreAccessorDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings)
    {
        return !settings.NoStore;
    }

    protected override void Generate(
        StoreAccessorDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        fileName = descriptor.Name;
        path = State;
        ns = descriptor.RuntimeType.NamespaceWithoutGlobal;

        var factory = ClassBuilder
            .New(fileName)
            .SetAccessModifier(settings.AccessModifier)
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

        factory.Build(writer);
    }
}
