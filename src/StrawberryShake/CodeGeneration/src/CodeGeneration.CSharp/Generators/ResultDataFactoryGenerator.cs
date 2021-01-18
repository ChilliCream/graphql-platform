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
    public class ResultDataFactoryGenerator : ClassBaseGenerator<TypeDescriptor>
    {
        const string StoreParamName = "_entityStore";

        protected override Task WriteAsync(CodeWriter writer, TypeDescriptor typeDescriptor)
        {
            AssertNonNull(
                writer,
                typeDescriptor);

            ClassBuilder
                .SetName(ResultFactoryNameFromTypeName(typeDescriptor.Name))
                .AddImplements($"{IOperationResultDataFactory}<{typeDescriptor.Name}>");

            ConstructorBuilder
                .SetTypeName(typeDescriptor.Name)
                .SetAccessModifier(AccessModifier.Public);

            AddConstructorAssignedField(
                WellKnownNames.IEntityStore,
                StoreParamName);

            var mappersToInject = typeDescriptor.Properties
                .Where(prop => !prop.Type.IsScalarType)
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
                    EntityMapperNameFromGraphQLTypeName(mapperType.Name, gqlTypeName).ToFieldName()
                );
            }

            var createMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetName("Create")
                .SetReturnType(typeDescriptor.Name)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetName("dataInfo")
                        .SetType(WellKnownNames.IOperationResultDataInfo)
                );

            var returnStatement = MethodCallBuilder.New()
                .SetPrefix("return new ")
                .SetMethodName(typeDescriptor.Name);

            var ifHasCorrectType = IfBuilder.New()
                .SetCondition(
                    $"dataInfo is {ResultInfoNameFromTypeName(typeDescriptor.Name)} info");

            foreach (var prop in typeDescriptor.Properties)
            {
                if (prop.Type.Kind == TypeKind.Scalar)
                {
                    returnStatement.AddArgument($"info.{prop.Name}");
                }
                else
                {
                    GenerateMappingAssignment(
                        ifHasCorrectType,
                        returnStatement,
                        prop);
                }
            }

            ifHasCorrectType.AddCode(returnStatement);
            createMethod.AddCode(ifHasCorrectType);
            createMethod.AddEmptyLine();
            createMethod.AddCode(
                "throw new ArgumentException(\"" +
                $"{ResultInfoNameFromTypeName(typeDescriptor.Name)} expected.\");");

            ClassBuilder.AddMethod(createMethod);

            return CodeFileBuilder.New()
                .SetNamespace(typeDescriptor.Namespace)
                .AddType(ClassBuilder)
                .BuildAsync(writer);
        }

        private void GenerateMappingAssignment(
            IfBuilder codeContainer,
            MethodCallBuilder returnBuilder,
            NamedTypeReferenceDescriptor prop)
        {
            switch (prop.Type)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    // TODO
                    break;

                case TypeDescriptor typeDescriptor1:
                    var idName = $"{prop.Name}Id";
                    var varName = prop.Name.WithLowerFirstChar();
                    if (typeDescriptor1.IsInterface)
                    {
                        codeContainer.AddCode($"{typeDescriptor1.Name} {varName} = default!;");
                        codeContainer.AddEmptyLine();

                        foreach (TypeDescriptor implementee in typeDescriptor1.IsImplementedBy)
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
                        if (prop.Type is TypeDescriptor nonList)
                        {
                            returnBuilder.AddArgument(GetMappingCall(nonList, idName));
                        }
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private MethodCallBuilder GetMappingCall(TypeDescriptor typeDescriptor, string idName)
        {
            return MethodCallBuilder.New()
                .SetMethodName(
                    EntityMapperNameFromGraphQLTypeName(
                        typeDescriptor.Name,
                        typeDescriptor.GraphQLTypeName))
                .SetDetermineStatement(false)
                .AddArgument(
                    $"{StoreParamName}.GetEntity<" +
                    $"{EntityTypeNameFromGraphQLTypeName(typeDescriptor.GraphQLTypeName)}>" +
                    $"(info.{idName})");
        }

        private IEnumerable<TypeDescriptor> GetMappers(ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor switch
            {
                ListTypeDescriptor listTypeDescriptor =>
                    GetMappers(listTypeDescriptor.InnerType),

                TypeDescriptor typeDescriptor1 =>
                    typeDescriptor1.IsInterface
                        ? typeDescriptor1.IsImplementedBy
                        : new[] { typeDescriptor1 },

                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
