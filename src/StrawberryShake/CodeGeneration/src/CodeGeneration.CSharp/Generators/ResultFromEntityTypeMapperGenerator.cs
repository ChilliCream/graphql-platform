using System;
using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class ResultFromEntityTypeMapperGenerator : ClassBaseGenerator<ITypeDescriptor>
    {
        const string _entityParamName = "entity";
        const string _storeFieldName = "_entityStore";
        const string _mapMethodName = "Map";

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


            NamedTypeDescriptor descriptor = (NamedTypeDescriptor)typeDescriptor.NamedType();

            // Setup class
            fileName = descriptor.ExtractMapperName();

            classBuilder
                .AddImplements(
                    TypeNames.IEntityMapper
                        .WithGeneric(descriptor.ExtractTypeName(), descriptor.Name))
                .SetName(fileName);

            constructorBuilder.SetTypeName(descriptor.Name);

            if (descriptor.ContainsEntity())
            {
                AddConstructorAssignedField(
                    TypeNames.IEntityStore,
                    _storeFieldName,
                    classBuilder,
                    constructorBuilder);
            }


            // Define map method
            MethodBuilder mapMethod = MethodBuilder.New()
                .SetName(_mapMethodName)
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(descriptor.Name)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(
                            descriptor.Kind == TypeKind.EntityType
                                ? EntityTypeNameFromGraphQLTypeName(descriptor.GraphQLTypeName)
                                : descriptor.Name)
                        .SetName(_entityParamName));

            var constructorCall = new MethodCallBuilder()
                .SetMethodName($"return new {descriptor.Name}");
            if (typeDescriptor is NamedTypeDescriptor namedTypeDescriptor)
            {
                foreach (PropertyDescriptor property in namedTypeDescriptor.Properties)
                {
                    if (property.Type.IsEntityType())
                    {
                        constructorCall.AddArgument($"{property.Name.WithLowerFirstChar()}Id");
                    }
                    else
                    {
                        constructorCall.AddArgument(BuildMapMethodCall(_entityParamName, property));
                    }
                }
            }


            var processed = new HashSet<string>();

            mapMethod.AddCode(constructorCall);

            if (constructorBuilder.HasParameters())
            {
                classBuilder.AddConstructor(constructorBuilder);
            }

            classBuilder.AddMethod(mapMethod);
            AddRequiredMapMethods(_entityParamName, descriptor, classBuilder, constructorBuilder, processed);

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
