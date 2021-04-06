using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class NoStoreAccessorGenerator : CodeGenerator<StoreAccessorDescriptor>
    {
        private const string _operationStore = "operationStore";
        private const string _entityStore = "entityStore";
        private const string _entityIdSerializer = "entityIdSerializer";
        private const string _requestFactories = "requestFactories";
        private const string _resultDataFactories = "resultDataFactories";

        protected override bool CanHandle(
            StoreAccessorDescriptor descriptor,
            CodeGeneratorSettings settings)
        {
            return settings.NoStore;
        }

        protected override void Generate(
            StoreAccessorDescriptor descriptor,
            CodeGeneratorSettings settings,
            CodeWriter writer,
            out string fileName,
            out string? path)
        {
            fileName = descriptor.Name;
            path = State;

            ClassBuilder factory = ClassBuilder
                .New(fileName)
                .SetAccessModifier(AccessModifier.Public)
                .AddImplements(TypeNames.IStoreAccessor);

            AddThrowNotValidWithoutStore(factory, "OperationStore", TypeNames.IOperationStore);
            AddThrowNotValidWithoutStore(factory, "EntityStore", TypeNames.IEntityStore);
            AddThrowNotValidWithoutStore(factory,
                "EntityIdSerializer",
                TypeNames.IEntityIdSerializer);

            factory
                .AddMethod("GetOperationRequestFactory")
                .SetPublic()
                .SetReturnType(TypeNames.IOperationRequestFactory)
                .AddParameter("resultType", x => x.SetType(TypeNames.Type))
                .AddCode(ExceptionBuilder
                    .New(TypeNames.NotSupportedException)
                    .AddArgument(
                        "\"GetOperationRequestFactory is not supported in store less mode\""));

            factory
                .AddMethod("GetOperationResultDataFactory")
                .SetPublic()
                .SetReturnType(TypeNames.IOperationResultDataFactory)
                .AddParameter("resultType", x => x.SetType(TypeNames.Type))
                .AddCode(ExceptionBuilder
                    .New(TypeNames.NotSupportedException)
                    .AddArgument(
                        "\"GetOperationResultDataFactory is not supported in store less mode\""));

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(factory)
                .Build(writer);
        }

        private void AddThrowNotValidWithoutStore(
            ClassBuilder classBuilder,
            string propertyName,
            string type)
        {
            classBuilder
                .AddProperty(propertyName)
                .SetPublic()
                .SetType(type)
                .AsLambda(ExceptionBuilder
                    .Inline(TypeNames.NotSupportedException)
                    .AddArgument($"\"{propertyName} is not supported in store less mode\""));
        }
    }
}
