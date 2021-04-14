using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class ResultFromEntityTypeMapperGenerator : TypeMapperGenerator
    {
        private const string _entity = "entity";
        private const string _entityStore = "_entityStore";
        private const string _map = "Map";
        private const string _snapshot = "snapshot";

        protected override bool CanHandle(ITypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings)
        {
            return descriptor.Kind == TypeKind.EntityType && !descriptor.IsInterface() &&
                !settings.NoStore;
        }

        protected override void Generate(ITypeDescriptor typeDescriptor,
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

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .AddImplements(
                    TypeNames.IEntityMapper
                        .WithGeneric(
                            descriptor.ExtractType().ToString(),
                            descriptor.RuntimeType.Name))
                .SetName(fileName);

            ConstructorBuilder constructorBuilder = ConstructorBuilder
                .New()
                .SetTypeName(descriptor.Name);

            AddConstructorAssignedField(
                TypeNames.IEntityStore,
                _entityStore,
                entityStore,
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
                            descriptor.Kind == TypeKind.EntityType
                                ? CreateEntityType(
                                        descriptor.Name,
                                        descriptor.RuntimeType.NamespaceWithoutGlobal)
                                    .ToString()
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
                        .SetRighthandSide($"{_entityStore}.CurrentSnapshot")))
                .AddEmptyLine();

            MethodCallBuilder constructorCall =
                MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetNew()
                    .SetMethodName(descriptor.RuntimeType.Name);

            if (typeDescriptor is ComplexTypeDescriptor complexTypeDescriptor)
            {
                foreach (PropertyDescriptor property in complexTypeDescriptor.Properties)
                {
                    constructorCall.AddArgument(BuildMapMethodCall(settings, _entity, property));
                }
            }

            mapMethod.AddCode(constructorCall);

            if (constructorBuilder.HasParameters())
            {
                classBuilder.AddConstructor(constructorBuilder);
            }

            classBuilder.AddMethod(mapMethod);

            AddRequiredMapMethods(
                settings,
                _entity,
                descriptor,
                classBuilder,
                constructorBuilder,
                new HashSet<string>());

            classBuilder.Build(writer);
        }
    }
}
