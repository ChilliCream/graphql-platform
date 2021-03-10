using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class StoreAccessorGenerator : CodeGenerator<StoreAccessorDescriptor>
    {
        private const string _operationStore = "operationStore";
        private const string _entityStore = "entityStore";

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
                .AddImplements(TypeNames.IStoreAccessor);

            factory
                .AddConstructor()
                .SetTypeName(fileName)
                .SetPublic()
                .AddParameter(_operationStore, x => x.SetType(TypeNames.IOperationStore))
                .AddParameter(_entityStore, x => x.SetType(TypeNames.IEntityStore))
                .AddCode(AssignmentBuilder
                    .New()
                    .SetLefthandSide(entityStore)
                    .SetRighthandSide(_entityStore))
                .AddCode(AssignmentBuilder
                    .New()
                    .SetLefthandSide("OperationStore")
                    .SetRighthandSide(_operationStore));

            factory
                .AddProperty(operationStore)
                .SetPublic()
                .SetType(TypeNames.IOperationStore);

            factory
                .AddProperty(entityStore)
                .SetPublic()
                .SetType(TypeNames.IEntityStore);

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(factory)
                .Build(writer);
        }
    }
}
