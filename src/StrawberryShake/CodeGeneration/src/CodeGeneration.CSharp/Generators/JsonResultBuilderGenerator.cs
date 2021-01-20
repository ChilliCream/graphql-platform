using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
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
        private const string _transportResultRootTypeName = "JsonElement";
        private const string _entityIdsParam = "entityIds";
        private const string _jsonElementParamName = "JsonElement?";
        private const string _objParamName = "obj";

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
                    return BuildDeserializeMethodName(listTypeDescriptor.InnerType, true) +
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
                        ? BuildDeserializeMethodName(nonNullTypeDescriptor.InnerType) + "NonNullable"
                        :"NonNullable" + BuildDeserializeMethodName(nonNullTypeDescriptor.InnerType);
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeDescriptor));
            }
        }

        protected override Task WriteAsync(
            CodeWriter writer,
            ResultBuilderDescriptor resultBuilderDescriptor)
        {
            AssertNonNull(
                writer,
                resultBuilderDescriptor);

            var resultTypeDescriptor = resultBuilderDescriptor.ResultNamedType;

            ClassBuilder.SetName(
                ResultBuilderNameFromTypeName(resultBuilderDescriptor.ResultNamedType.Name));

            ConstructorBuilder.SetTypeName(
                ResultBuilderNameFromTypeName(resultBuilderDescriptor.ResultNamedType.Name));

            ClassBuilder.AddImplements(
                $"{WellKnownNames.IOperationResultBuilder}<{_transportResultRootTypeName}," +
                $" {resultTypeDescriptor.Name}>");

            AddConstructorAssignedField(
                WellKnownNames.IEntityStore,
                _entityStoreFieldName);

            AddConstructorAssignedField(
                TypeReferenceBuilder.New()
                    .SetName("Func")
                    .AddGeneric(_transportResultRootTypeName)
                    .AddGeneric(WellKnownNames.EntityId),
                _extractIdFieldName);

            AddConstructorAssignedField(
                TypeReferenceBuilder.New()
                    .SetName(WellKnownNames.IOperationResultDataFactory)
                    .AddGeneric(resultTypeDescriptor.Name),
                _resultDataFactoryFieldName);

            ConstructorBuilder.AddParameter(
                ParameterBuilder.New()
                    .SetName(_serializerResolverParamName)
                    .SetType(WellKnownNames.ISerializerResolver));

            foreach (var valueParser in resultBuilderDescriptor.ValueParsers)
            {
                var parserFieldName = $"_{valueParser.RuntimeType}Parser";
                ClassBuilder.AddField(
                    FieldBuilder.New().SetName(parserFieldName).SetType(
                        TypeReferenceBuilder.New()
                            .SetName(WellKnownNames.ILeafValueParser)
                            .AddGeneric(valueParser.SerializedType)
                            .AddGeneric(valueParser.RuntimeType)));

                ConstructorBuilder.AddCode(
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

            AddBuildMethod(resultTypeDescriptor);

            AddBuildDataMethod(resultTypeDescriptor);

            AddRequiredDeserializeMethods(resultBuilderDescriptor.ResultNamedType);

            return CodeFileBuilder.New()
                .SetNamespace(resultBuilderDescriptor.ResultNamedType.Namespace)
                .AddType(ClassBuilder)
                .BuildAsync(writer);
        }

        /// <summary>
        /// Adds all required deserializers of the given type descriptors properties
        /// </summary>
        private void AddRequiredDeserializeMethods(NamedTypeDescriptor namedTypeDescriptor)
        {
            foreach (var property in namedTypeDescriptor.Properties)
            {
                AddDeserializeMethod(property.Type);
            }
        }

        private void AddDeserializeMethod(ITypeDescriptor typeReference, ITypeDescriptor? originalTypeReference = null)
        {
            var originalTypeDescriptor = originalTypeReference ?? typeReference;
            switch (typeReference)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    AddUpdateEntityArrayMethod(
                        listTypeDescriptor,
                        originalTypeDescriptor);
                    break;

                case NamedTypeDescriptor typeDescriptor:
                    switch (typeDescriptor.Kind)
                    {
                        case TypeKind.LeafType:
                            AddScalarTypeDeserializerMethod(
                                typeDescriptor,
                                originalTypeDescriptor);
                            break;
                        case TypeKind.DataType:
                            AddDataTypeDeserializerMethod(
                                typeDescriptor,
                                originalTypeDescriptor);
                            break;
                        case TypeKind.EntityType:
                            AddUpdateEntityMethod(
                                typeDescriptor,
                                originalTypeDescriptor);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    AddDeserializeMethod(nonNullTypeDescriptor.InnerType, originalTypeDescriptor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeReference));
            }
        }

        private void AddBuildMethod(NamedTypeDescriptor resultNamedType)
        {
            var responseParameterName = "response";
            var buildMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetName("Build")
                .SetReturnType(
                    TypeReferenceBuilder.New()
                        .SetName(WellKnownNames.IOperationResult)
                        .AddGeneric(resultNamedType.Name))
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(
                            TypeReferenceBuilder.New()
                                .SetName(WellKnownNames.Response)
                                .AddGeneric("JsonDocument")
                                .SetName(WellKnownNames.Response))
                        .SetName(responseParameterName));

            buildMethod.AddCode(
                AssignmentBuilder.New()
                    .SetLefthandSide(
                        $"({resultNamedType.Name} Result, " +
                        $"{NamingConventions.ResultInfoNameFromTypeName(resultNamedType.Name)} " +
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
                                " out JsonElement obj)"))
                    .AddCode("data = BuildData(obj);"));

            buildMethod.AddEmptyLine();
            buildMethod.AddCode(
                MethodCallBuilder.New()
                    .SetPrefix("return new ")
                    .SetMethodName($"{WellKnownNames.OperationResult}<{resultNamedType.Name}>")
                    .AddArgument("data?.Result")
                    .AddArgument("data?.Info")
                    .AddArgument(_resultDataFactoryFieldName)
                    .AddArgument("null"));

            ClassBuilder.AddMethod(buildMethod);
        }

        private void AddScalarTypeDeserializerMethod(NamedTypeDescriptor namedType, ITypeDescriptor originalTypeDescriptor)
        {
            var scalarDeserializer = MethodBuilder.New()
                .SetName(DeserializerMethodNameFromTypeName(originalTypeDescriptor))
                .SetReturnType(namedType.Name)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType("JsonElement")
                        .SetName(_objParamName));

            scalarDeserializer.AddCode(
                EnsureJsonValueIsNotNull(),
                originalTypeDescriptor.IsNonNullableType());

            scalarDeserializer.AddCode(
                $"return {namedType.Name.ToFieldName()}Parser.Parse({_objParamName}" +
                $".Get{namedType.Name.WithCapitalFirstChar()}()!);");

            ClassBuilder.AddMethod(scalarDeserializer);
        }

        private CodeBlockBuilder EnsureJsonValueIsNotNull(string propertyName = _objParamName)
        {
            return CodeBlockBuilder.New()
                .AddCode(
                    IfBuilder.New()
                        .SetCondition(
                            ConditionBuilder.New()
                                .Set($"{propertyName} == null"))
                        .AddCode("throw new InvalidOperationException();"))
                .AddEmptyLine();
        }

        private MethodCallBuilder BuildUpdateMethodCall(PropertyDescriptor property)
        {
            var deserializeMethodCaller = MethodCallBuilder.New()
                .SetDetermineStatement(false)
                .SetMethodName(DeserializerMethodNameFromTypeName(property.Type));

            deserializeMethodCaller.AddArgument(
                $"{_objParamName}.GetPropertyOrNull(\"{property.Name.WithLowerFirstChar()}\")");

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
    }
}
