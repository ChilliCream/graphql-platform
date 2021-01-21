using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;
using static StrawberryShake.CodeGeneration.CSharp.WellKnownNames;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultDataFactoryGenerator : ClassBaseGenerator<NamedTypeDescriptor>
    {
        const string StoreParamName = "_entityStore";

        protected override bool CanHandle(NamedTypeDescriptor descriptor)
        {
            return descriptor.Kind == TypeKind.ResultType && !descriptor.IsInterface();
        }

        protected override void Generate(CodeWriter writer, NamedTypeDescriptor descriptor)
        {
            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            classBuilder
                .SetName(ResultFactoryNameFromTypeName(descriptor.Name))
                .AddImplements($"{IOperationResultDataFactory}<{descriptor.Name}>");

            constructorBuilder
                .SetTypeName(descriptor.Name)
                .SetAccessModifier(AccessModifier.Public);

            AddConstructorAssignedField(
                WellKnownNames.IEntityStore,
                StoreParamName,
                classBuilder,
                constructorBuilder);

            var mappersToInject = descriptor.Properties
                .Where(prop => !prop.Type.IsLeafType())
                .SelectMany(prop => GetMappers(prop.Type));

            foreach (var mapperType in mappersToInject)
            {
                var gqlTypeName = mapperType.GraphQLTypeName ?? throw new ArgumentNullException();

                var typeName = TypeReferenceBuilder
                    .New()
                    .SetName(WellKnownNames.IEntityMapper)
                    .AddGeneric(EntityTypeNameFromGraphQLTypeName(gqlTypeName))
                    .AddGeneric(mapperType.Name);

                AddConstructorAssignedField(
                    typeName,
                    EntityMapperNameFromGraphQLTypeName(mapperType.Name, gqlTypeName)
                        .ToFieldName(),
                    classBuilder,
                    constructorBuilder);
            }

            var createMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetName("Create")
                .SetReturnType(descriptor.Name)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetName("dataInfo")
                        .SetType(WellKnownNames.IOperationResultDataInfo)
                );

            var returnStatement = MethodCallBuilder.New()
                .SetPrefix("return new ")
                .SetMethodName(descriptor.Name);

            var ifHasCorrectType = IfBuilder.New()
                .SetCondition(
                    $"dataInfo is {ResultInfoNameFromTypeName(descriptor.Name)} info");

            foreach (var prop in descriptor.Properties)
            {
                if (prop.Type.Kind == TypeKind.LeafType)
                {
                    returnStatement.AddArgument($"info.{prop.Name}");
                }
                else
                {
                    GenerateMappingAssignment(
                        ifHasCorrectType,
                        returnStatement,
                        prop,
                        prop.Type);
                }
            }

            ifHasCorrectType.AddCode(returnStatement);
            createMethod.AddCode(ifHasCorrectType);
            createMethod.AddEmptyLine();
            createMethod.AddCode(
                "throw new ArgumentException(\"" +
                $"{ResultInfoNameFromTypeName(descriptor.Name)} expected.\");");

            classBuilder.AddMethod(createMethod);

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }

        private void GenerateMappingAssignment(
            IfBuilder codeContainer,
            MethodCallBuilder returnBuilder,
            PropertyDescriptor propertyDescriptor,
            ITypeDescriptor typeDescriptor)
        {
            switch (typeDescriptor)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    // TODO
                    break;

                case NamedTypeDescriptor namedType:
                    var idName = $"{propertyDescriptor.Name}Id";
                    var varName = propertyDescriptor.Name.WithLowerFirstChar();
                    if (namedType.IsInterface)
                    {
                        codeContainer.AddCode($"{namedType.Name} {varName} = default!;");
                        codeContainer.AddEmptyLine();

                        foreach (NamedTypeDescriptor implementee in namedType.ImplementedBy)
                        {
                            codeContainer.AddCode(
                                IfBuilder.New()
                                    .SetCondition(
                                        ConditionBuilder.New().Set(
                                            MethodCallBuilder.New()
                                                .SetDetermineStatement(false)
                                                .SetMethodName($"info.{idName}.Name.Equals")
                                                .AddArgument($"\"{implementee.GraphQLTypeName}\"")
                                                .AddArgument("StringComparison.Ordinal")))
                                    .AddCode(AssignmentBuilder.New()
                                        .SetLefthandSide(varName)
                                        .SetRighthandSide(GetMappingCall(implementee, idName))));

                            codeContainer.AddEmptyLine();
                        }

                        returnBuilder.AddArgument(varName);
                    }
                    else
                    {
                        if (typeDescriptor is NamedTypeDescriptor nonList)
                        {
                            returnBuilder.AddArgument(GetMappingCall(nonList, idName));
                        }
                    }
                    break;

                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    GenerateMappingAssignment(
                        codeContainer,
                        returnBuilder,
                        propertyDescriptor,
                        nonNullTypeDescriptor.InnerType);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private MethodCallBuilder GetMappingCall(NamedTypeDescriptor namedTypeDescriptor, string idName)
        {
            return MethodCallBuilder.New()
                .SetMethodName(
                    EntityMapperNameFromGraphQLTypeName(
                        namedTypeDescriptor.Name,
                        namedTypeDescriptor.GraphQLTypeName))
                .SetDetermineStatement(false)
                .AddArgument(
                    $"{StoreParamName}.GetEntity<" +
                    $"{EntityTypeNameFromGraphQLTypeName(namedTypeDescriptor.GraphQLTypeName)}>" +
                    $"(info.{idName})");
        }

        private IEnumerable<NamedTypeDescriptor> GetMappers(ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor switch
            {
                ListTypeDescriptor listTypeDescriptor =>
                    GetMappers(listTypeDescriptor.InnerType),

                NamedTypeDescriptor typeDescriptor1 =>
                    typeDescriptor1.IsInterface
                        ? typeDescriptor1.ImplementedBy
                        : new[] { typeDescriptor1 },

                NonNullTypeDescriptor nonNullTypeDescriptor =>
                    GetMappers(nonNullTypeDescriptor.InnerType),

                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
