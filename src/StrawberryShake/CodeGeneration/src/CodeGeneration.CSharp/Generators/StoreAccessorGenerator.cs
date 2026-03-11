using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class StoreAccessorGenerator : CodeGenerator<StoreAccessorDescriptor>
{
    private const string OperationStore = "operationStore";
    private const string EntityStore = "entityStore";
    private const string EntityIdSerializer = "entityIdSerializer";
    private const string RequestFactories = "requestFactories";
    private const string ResultDataFactories = "resultDataFactories";

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
            .AddParameter(OperationStore, x => x.SetType(TypeNames.IOperationStore))
            .AddParameter(EntityStore, x => x.SetType(TypeNames.IEntityStore))
            .AddParameter(EntityIdSerializer, x => x.SetType(TypeNames.IEntityIdSerializer))
            .AddParameter(
                RequestFactories,
                x => x.SetType(
                    TypeNames.IEnumerable.WithGeneric(TypeNames.IOperationRequestFactory)))
            .AddParameter(
                ResultDataFactories,
                x => x.SetType(
                    TypeNames.IEnumerable.WithGeneric(TypeNames.IOperationResultDataFactory)))
            .AddBase(OperationStore)
            .AddBase(EntityStore)
            .AddBase(EntityIdSerializer)
            .AddBase(RequestFactories)
            .AddBase(ResultDataFactories);

        factory.Build(writer);
    }
}
