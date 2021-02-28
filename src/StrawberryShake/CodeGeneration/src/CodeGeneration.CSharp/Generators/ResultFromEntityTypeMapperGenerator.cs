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
        private const string _entityParamName = "entity";
        private const string _mapMethodName = "Map";

        protected override bool CanHandle(ITypeDescriptor descriptor)
        {
            return descriptor.Kind == TypeKind.EntityType && !descriptor.IsInterface();
        }

        protected override void Generate(
            CodeWriter writer,
            ITypeDescriptor typeDescriptor,
            out string fileName)
        {
            var (classBuilder, constructorBuilder) = CreateClassBuilder(false);

            ComplexTypeDescriptor descriptor =
                typeDescriptor as ComplexTypeDescriptor ??
                throw new InvalidOperationException(
                    "A result entity mapper can only be generated for complex types");

            // Setup class
            fileName = descriptor.ExtractMapperName();

            classBuilder
                .AddImplements(
                    TypeNames.IEntityMapper
                        .WithGeneric(descriptor.ExtractTypeName(), descriptor.RuntimeType.Name))
                .SetName(fileName);

            constructorBuilder.SetTypeName(descriptor.Name);

            if (descriptor.ContainsEntity())
            {
                AddConstructorAssignedField(
                    TypeNames.IEntityStore,
                    StoreFieldName,
                    classBuilder,
                    constructorBuilder);
            }

            // Define map method
            MethodBuilder mapMethod = MethodBuilder.New()
                .SetName(_mapMethodName)
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(descriptor.RuntimeType.Name)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(
                            descriptor.Kind == TypeKind.EntityType
                                ? CreateEntityTypeName(descriptor.Name)
                                : descriptor.Name)
                        .SetName(_entityParamName));

            var constructorCall =
                MethodCallBuilder
                    .New()
                    .SetMethodName($"return new {descriptor.RuntimeType.Name}");

            if (typeDescriptor is ComplexTypeDescriptor complexTypeDescriptor)
            {
                foreach (PropertyDescriptor property in complexTypeDescriptor.Properties)
                {
                    constructorCall.AddArgument(BuildMapMethodCall(_entityParamName, property));
                }
            }

            mapMethod.AddCode(constructorCall);

            if (constructorBuilder.HasParameters())
            {
                classBuilder.AddConstructor(constructorBuilder);
            }

            classBuilder.AddMethod(mapMethod);

            AddRequiredMapMethods(
                _entityParamName,
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
