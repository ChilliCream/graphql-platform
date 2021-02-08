using System;
using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator : ClassBaseGenerator<ResultBuilderDescriptor>
    {
        private const string _entityStoreFieldName = "_entityStore";
        private const string _extractIdFieldName = "_extractId";
        private const string _resultDataFactoryFieldName = "_resultDataFactory";
        private const string _serializerResolverParamName = "serializerResolver";
        private const string _entityIdsParam = "entityIds";
        private const string _jsonElementParamName = TypeNames.JsonElement + "?";
        private const string _objParamName = "obj";

        protected override void Generate(
            CodeWriter writer,
            ResultBuilderDescriptor resultBuilderDescriptor, out string fileName)
        {
            var resultTypeDescriptor = resultBuilderDescriptor.ResultNamedType;
            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            fileName = resultBuilderDescriptor.Name;
            classBuilder.SetName(fileName);

            constructorBuilder.SetTypeName(fileName);

            classBuilder.AddImplements(
                $"{TypeNames.IOperationResultBuilder}<{TypeNames.JsonDocument}," +
                $" {resultTypeDescriptor.Name}>");

            AddConstructorAssignedField(
                TypeNames.IEntityStore,
                _entityStoreFieldName,
                classBuilder,
                constructorBuilder);

            AddConstructorAssignedField(
                TypeReferenceBuilder.New()
                    .SetName(TypeNames.Func)
                    .AddGeneric(TypeNames.JsonElement)
                    .AddGeneric(TypeNames.EntityId),
                _extractIdFieldName,
                classBuilder,
                constructorBuilder);

            AddConstructorAssignedField(
                TypeReferenceBuilder.New()
                    .SetName(TypeNames.IOperationResultDataFactory)
                    .AddGeneric(resultTypeDescriptor.Name),
                _resultDataFactoryFieldName,
                classBuilder,
                constructorBuilder);

            constructorBuilder.AddParameter(
                ParameterBuilder.New()
                    .SetName(_serializerResolverParamName)
                    .SetType(TypeNames.ISerializerResolver));

            foreach (var valueParser in resultBuilderDescriptor.ValueParsers)
            {
                var parserFieldName =
                    $"_{valueParser.RuntimeType.Split('.').Last().WithLowerFirstChar()}Parser";
                classBuilder.AddField(
                    FieldBuilder.New().SetName(parserFieldName).SetType(
                        TypeReferenceBuilder.New()
                            .SetName(TypeNames.ILeafValueParser)
                            .AddGeneric(valueParser.SerializedType)
                            .AddGeneric(valueParser.RuntimeType)));

                constructorBuilder.AddCode(
                    AssignmentBuilder.New()
                        .AssertNonNull(parserFieldName)
                        .SetLefthandSide(parserFieldName)
                        .SetRighthandSide(
                            MethodCallBuilder.New()
                                .SetPrefix(_serializerResolverParamName + ".")
                                .SetDetermineStatement(false)
                                .SetMethodName(
                                    $"GetLeafValueParser<{valueParser.SerializedType}, " +
                                    $"{valueParser.RuntimeType}>")
                                .AddArgument($"\"{valueParser.GraphQLTypeName}\"")));
            }

            AddBuildMethod(
                resultTypeDescriptor,
                classBuilder);

            AddBuildDataMethod(
                resultTypeDescriptor,
                classBuilder);

            AddRequiredDeserializeMethods(
                resultBuilderDescriptor.ResultNamedType,
                classBuilder);

            CodeFileBuilder.New()
                .SetNamespace(resultBuilderDescriptor.ResultNamedType.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }

        /// <summary>
        /// Adds all required deserializers of the given type descriptors properties
        /// </summary>
        private void AddRequiredDeserializeMethods(
            NamedTypeDescriptor namedTypeDescriptor,
            ClassBuilder classBuilder)
        {
            foreach (var property in namedTypeDescriptor.Properties)
            {
                AddDeserializeMethod(
                    property.Type,
                    classBuilder);
            }
        }

        private void AddDeserializeMethod(
            ITypeDescriptor typeReference,
            ClassBuilder classBuilder)
        {
            var returnType = typeReference.ToEntityIdBuilder();

            var methodBuilder = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Private)
                .SetName(DeserializerMethodNameFromTypeName(typeReference))
                .SetReturnType(returnType)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(_jsonElementParamName)
                        .SetName(_objParamName));
            if (typeReference.IsEntityType())
            {
                methodBuilder.AddParameter(
                    ParameterBuilder.New()
                        .SetType($"{TypeNames.ISet}<{TypeNames.EntityId}>")
                        .SetName(_entityIdsParam));
            }

            methodBuilder.AddCode(
                EnsureProperNullability(isNonNullType: typeReference.IsNonNullableType()));

            classBuilder.AddMethod(methodBuilder);

            AddDeserializeMethodBody(
                classBuilder,
                methodBuilder,
                typeReference);
        }

        private void AddDeserializeMethodBody(ClassBuilder classBuilder,
            MethodBuilder methodBuilder,
            ITypeDescriptor typeDescriptor)
        {
            switch (typeDescriptor)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    AddArrayHandler(
                        classBuilder,
                        methodBuilder,
                        listTypeDescriptor);
                    break;
                case NamedTypeDescriptor namedTypeDescriptor:
                    switch (typeDescriptor.Kind)
                    {
                        case TypeKind.LeafType:
                            AddScalarTypeDeserializerMethod(
                                methodBuilder,
                                namedTypeDescriptor);
                            break;

                        case TypeKind.DataType:
                            AddDataTypeDeserializerMethod(
                                classBuilder,
                                methodBuilder,
                                namedTypeDescriptor);
                            break;

                        case TypeKind.EntityType:
                            AddUpdateEntityMethod(
                                classBuilder,
                                methodBuilder,
                                namedTypeDescriptor);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    AddDeserializeMethodBody(
                        classBuilder,
                        methodBuilder,
                        nonNullTypeDescriptor.InnerType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeDescriptor));
            }
        }

        private void AddBuildMethod(
            NamedTypeDescriptor resultNamedType,
            ClassBuilder classBuilder)
        {
            var responseParameterName = "response";

            var buildMethod = MethodBuilder
                .New()
                .SetAccessModifier(AccessModifier.Public)
                .SetName("Build")
                .SetReturnType(
                    TypeReferenceBuilder.New()
                        .SetName(TypeNames.IOperationResult)
                        .AddGeneric(resultNamedType.Name))
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(
                            TypeReferenceBuilder.New()
                                .SetName(TypeNames.Response)
                                .AddGeneric(TypeNames.JsonDocument)
                                .SetName(TypeNames.Response))
                        .SetName(responseParameterName));

            buildMethod.AddCode(
                AssignmentBuilder.New()
                    .SetLefthandSide(
                        $"({resultNamedType.Name} Result, " +
                        $"{ResultInfoNameFromTypeName(resultNamedType.ImplementedBy[0].Name)} " +
                        "Info)? data")
                    .SetRighthandSide("null"));

            buildMethod.AddEmptyLine();
            buildMethod.AddCode(
                IfBuilder.New()
                    .SetCondition(
                        ConditionBuilder.New()
                            .Set("response.Body is not null")
                            .And(
                                "response.Body.RootElement.TryGetProperty(\"data\"," +
                                $" out {TypeNames.JsonElement} obj)"))
                    .AddCode("data = BuildData(obj);"));

            buildMethod.AddEmptyLine();
            buildMethod.AddCode(
                MethodCallBuilder.New()
                    .SetPrefix("return new ")
                    .SetMethodName($"{TypeNames.OperationResult}<{resultNamedType.Name}>")
                    .AddArgument("data?.Result")
                    .AddArgument("data?.Info")
                    .AddArgument(_resultDataFactoryFieldName)
                    .AddArgument("null"));

            classBuilder.AddMethod(buildMethod);
        }

        private CodeBlockBuilder EnsureProperNullability(
            string propertyName = _objParamName,
            bool isNonNullType = false)
        {
            var ifBuilder = IfBuilder
                .New()
                .SetCondition(
                    ConditionBuilder.New()
                        .Set($"!{propertyName}.HasValue"));
            ifBuilder.AddCode(
                isNonNullType
                    ? $"throw new {TypeNames.ArgumentNullException}();"
                    : "return null;");

            var codeBuilder = CodeBlockBuilder.New()
                .AddCode(ifBuilder)
                .AddEmptyLine();

            return codeBuilder;
        }

        private MethodCallBuilder BuildUpdateMethodCall(
            PropertyDescriptor property,
            string propertyAccess = ".Value")
        {
            var deserializeMethodCaller =
                MethodCallBuilder
                    .New()
                    .SetDetermineStatement(false)
                    .SetMethodName(DeserializerMethodNameFromTypeName(property.Type));

            deserializeMethodCaller.AddArgument(
                $"{TypeNames.GetPropertyOrNull}({_objParamName}{propertyAccess}, " +
                $"\"{property.Name.WithLowerFirstChar()}\")");

            if (!property.Type.IsLeafType())
            {
                deserializeMethodCaller.AddArgument(_entityIdsParam);
            }

            return deserializeMethodCaller;
        }

        private MethodCallBuilder BuildUpdateMethodCall(ITypeDescriptor property, string firstArg)
        {
            var deserializeMethodCaller = MethodCallBuilder.New()
                .SetDetermineStatement(false)
                .SetMethodName(DeserializerMethodNameFromTypeName(property));

            deserializeMethodCaller.AddArgument(firstArg);

            if (!property.IsLeafType())
            {
                deserializeMethodCaller.AddArgument(_entityIdsParam);
            }

            return deserializeMethodCaller;
        }

        private static string DeserializerMethodNameFromTypeName(ITypeDescriptor typeDescriptor)
        {
            var ret = typeDescriptor.IsEntityType() ? "Update" : "Deserialize";
            ret += BuildDeserializeMethodName(typeDescriptor);
            return ret;
        }

        private static string BuildDeserializeMethodName(
            ITypeDescriptor typeDescriptor,
            bool parentIsList = false)
        {
            switch (typeDescriptor)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    return BuildDeserializeMethodName(
                               listTypeDescriptor.InnerType,
                               true) +
                           "Array";

                case NamedTypeDescriptor namedTypeDescriptor:
                    return namedTypeDescriptor.Kind switch
                    {
                        TypeKind.LeafType => typeDescriptor.Name.WithCapitalFirstChar(),
                        TypeKind.DataType => DataTypeNameFromTypeName(typeDescriptor.Name),
                        TypeKind.EntityType => EntityTypeNameFromGraphQLTypeName(
                            typeDescriptor.Name),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    return parentIsList
                        ? BuildDeserializeMethodName(nonNullTypeDescriptor.InnerType) +
                          "NonNullable"
                        : "NonNullable" +
                          BuildDeserializeMethodName(nonNullTypeDescriptor.InnerType);
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeDescriptor));
            }
        }
    }
}
