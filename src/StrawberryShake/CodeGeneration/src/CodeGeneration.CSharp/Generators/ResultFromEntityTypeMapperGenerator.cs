using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultFromEntityTypeMapperGenerator : TypeMapperGenerator
    {
        private const string _entity = "entity";
        private const string _map = "Map";

        protected override bool CanHandle(ITypeDescriptor descriptor)
        {
            return descriptor.Kind == TypeKind.EntityType && !descriptor.IsInterface();
        }

        protected override void Generate(
            CodeWriter writer,
            ITypeDescriptor typeDescriptor,
            out string fileName)
        {
            // Setup class
            ComplexTypeDescriptor descriptor =
                typeDescriptor as ComplexTypeDescriptor ??
                throw new InvalidOperationException(
                    "A result entity mapper can only be generated for complex types");
            fileName = descriptor.ExtractMapperName();

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .AddImplements(
                    TypeNames.IEntityMapper
                        .WithGeneric(descriptor.ExtractTypeName(), descriptor.RuntimeType.Name))
                .SetName(fileName);

            ConstructorBuilder constructorBuilder = ConstructorBuilder
                .New()
                .SetTypeName(descriptor.Name);

            if (descriptor.ContainsEntity())
            {
                AddConstructorAssignedField(
                    TypeNames.IEntityStore,
                    StoreFieldName,
                    classBuilder,
                    constructorBuilder);
            }

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
                                ? CreateEntityTypeName(descriptor.Name)
                                : descriptor.Name)
                        .SetName(_entity));

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
                    constructorCall.AddArgument(BuildMapMethodCall(_entity, property));
                }
            }

            mapMethod.AddCode(constructorCall);

            if (constructorBuilder.HasParameters())
            {
                classBuilder.AddConstructor(constructorBuilder);
            }

            classBuilder.AddMethod(mapMethod);

            AddRequiredMapMethods(
                _entity,
                descriptor,
                classBuilder,
                constructorBuilder,
                new HashSet<string>());

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
