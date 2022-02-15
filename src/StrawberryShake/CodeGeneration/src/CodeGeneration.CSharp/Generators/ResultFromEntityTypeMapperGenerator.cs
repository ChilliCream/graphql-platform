using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class ResultFromEntityTypeMapperGenerator : TypeMapperGenerator
    {
        private const string _entity = "entity";
        private const string _entityStore = "entityStore";
        private const string _map = "Map";
        private const string _snapshot = "snapshot";

        protected override bool CanHandle(
            ITypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings)
            => descriptor.Kind == TypeKind.Entity &&
               !descriptor.IsInterface() &&
               !settings.NoStore;

        protected override void Generate(
            ITypeDescriptor typeDescriptor,
            CSharpSyntaxGeneratorSettings settings,
            CodeWriter writer,
            out string fileName,
            out string? path,
            out string ns)
        {
            // Setup class
            ComplexTypeDescriptor descriptor =
                typeDescriptor as ComplexTypeDescriptor ??
                throw new InvalidOperationException(
                    "A result entity mapper can only be generated for complex types");

            fileName = descriptor.ExtractMapperName();
            path = State;
            ns = CreateStateNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal);
            RuntimeTypeInfo entityType = CreateEntityType(
                descriptor.Name,
                descriptor.RuntimeType.NamespaceWithoutGlobal);

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .AddImplements(
                    TypeNames.IEntityMapper.WithGeneric(
                        descriptor.ExtractType().ToString(),
                        descriptor.RuntimeType.Name))
                .SetName(fileName);

            ConstructorBuilder constructorBuilder = ConstructorBuilder
                .New()
                .SetTypeName(descriptor.Name);

            AddConstructorAssignedField(
                TypeNames.IEntityStore,
                GetFieldName(_entityStore),
                _entityStore,
                classBuilder,
                constructorBuilder);

            // Define map method
            MethodBuilder mapMethod = MethodBuilder
                .New()
                .SetName(_map)
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(descriptor.RuntimeType.Name)
                .AddParameter(
                    ParameterBuilder
                        .New()
                        .SetType(
                            descriptor.Kind is TypeKind.Entity
                                ? entityType.NamespaceWithoutGlobal
                                : descriptor.Name)
                        .SetName(_entity))
                .AddParameter(
                    _snapshot,
                    b => b.SetDefault("null")
                        .SetType(TypeNames.IEntityStoreSnapshot.MakeNullable()));

            mapMethod
                .AddCode(IfBuilder
                    .New()
                    .SetCondition($"{_snapshot} is null")
                    .AddCode(AssignmentBuilder
                        .New()
                        .SetLefthandSide(_snapshot)
                        .SetRighthandSide($"{GetFieldName(_entityStore)}.CurrentSnapshot")))
                .AddEmptyLine();

            // creates the instance of the model that is being mapped.
            MethodCallBuilder createModelCall =
                MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetNew()
                    .SetMethodName(descriptor.RuntimeType.Name);

            foreach (PropertyDescriptor property in descriptor.Properties)
            {
                createModelCall.AddArgument(BuildMapMethodCall(settings, _entity, property));
            }

            mapMethod.AddCode(createModelCall);
            classBuilder.AddConstructor(constructorBuilder);

            classBuilder.AddMethod(mapMethod);

            var processed = new HashSet<string>();

            AddRequiredMapMethods(
                settings,
                descriptor,
                classBuilder,
                constructorBuilder,
                processed);

            foreach (DeferredFragmentDescriptor deferred in descriptor.Deferred)
            {
                createModelCall.AddArgument(
                    MethodCallBuilder
                        .Inline()
                        .SetMethodName($"{_map}{deferred.Class.RuntimeType.Name}")
                        .AddArgument(_entity)
                        .AddArgument(_snapshot));

                AddMapFragmentMethod(
                    classBuilder,
                    constructorBuilder,
                    deferred.Class,
                    deferred.InterfaceName,
                    entityType.FullName,
                    processed);
            }

            classBuilder.Build(writer);
        }
    }
}
