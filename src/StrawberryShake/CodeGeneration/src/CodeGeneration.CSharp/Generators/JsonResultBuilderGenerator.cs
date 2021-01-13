using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator : ClassBaseGenerator<ResultBuilderDescriptor>
    {
        private const string EntityStoreFieldName = "_entityStore";
        private const string ExtractIdFieldName = "_extractId";
        private const string ResultDataFactoryFieldName = "_resultDataFactory";
        private const string SerializerResolverParamName = "serializerResolver";
        private const string TransportResultRootTypeName = "JsonElement";
        private const string EntityIdsParam = "entityIds";
        private const string jsonElementParamName = "JsonElement?";
        private const string objParamName = "obj";

        private static string DeserializerMethodNameFromTypeName(ITypeDescriptor typeDescriptor)
        {
            var ret = typeDescriptor.IsEntityType ? "Update" : "Deserialize";

            if (typeDescriptor.IsNullable)
            {
                ret += "Nullable";
            }
            else
            {
                ret += "NonNullable";
            }

            ret += typeDescriptor.Kind switch
            {
                TypeKind.Scalar => typeDescriptor.Name.WithCapitalFirstChar(),
                TypeKind.DataType => NamingConventions.DataTypeNameFromTypeName(typeDescriptor.Name),
                TypeKind.EntityType => NamingConventions.EntityTypeNameFromTypeName(typeDescriptor.Name),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (typeDescriptor is ListTypeDescriptor listTypeDescriptor)
            {
                ret += typeDescriptor.IsNullable switch
                {
                    true => "Nullable",
                    false => "NonNullable",
                };

                ret += "Array";
            };



            return ret;
        }

        protected override Task WriteAsync(CodeWriter writer, ResultBuilderDescriptor resultBuilderDescriptor)
        {
            AssertNonNull(
                writer,
                resultBuilderDescriptor
            );

            var resultTypeDescriptor = resultBuilderDescriptor.ResultType;

            ClassBuilder.SetName(
                NamingConventions.ResultBuilderNameFromTypeName(resultBuilderDescriptor.ResultType.Name)
            );
            ConstructorBuilder.SetTypeName(
                NamingConventions.ResultBuilderNameFromTypeName(resultBuilderDescriptor.ResultType.Name)
            );
            ClassBuilder.AddImplements(
                $"{WellKnownNames.IOperationResultBuilder}<{TransportResultRootTypeName}, {resultTypeDescriptor.Name}>"
            );

            AddConstructorAssignedField(
                WellKnownNames.IEntityStore,
                EntityStoreFieldName
            );
            AddConstructorAssignedField(
                TypeReferenceBuilder.New()
                    .SetName("Func")
                    .AddGeneric(TransportResultRootTypeName)
                    .AddGeneric(WellKnownNames.EntityId),
                ExtractIdFieldName
            );
            AddConstructorAssignedField(
                TypeReferenceBuilder.New()
                    .SetName(WellKnownNames.IOperationResultDataFactory)
                    .AddGeneric(resultTypeDescriptor.Name),
                ResultDataFactoryFieldName
            );

            ConstructorBuilder.AddParameter(
                ParameterBuilder.New()
                    .SetName(SerializerResolverParamName)
                    .SetType(WellKnownNames.ISerializerResolver)
            );

            foreach (var valueParser in resultBuilderDescriptor.ValueParsers)
            {
                var parserFieldName = $"_{valueParser.runtimeType}Parser";
                ClassBuilder.AddField(
                    FieldBuilder.New().SetName(parserFieldName).SetType(
                        TypeReferenceBuilder.New()
                            .SetName(WellKnownNames.ILeafValueParser)
                            .AddGeneric(valueParser.serializedType)
                            .AddGeneric(valueParser.runtimeType)
                    )
                );

                ConstructorBuilder.AddCode(
                    AssignmentBuilder.New()
                        .AssertNonNull(parserFieldName)
                        .SetLefthandSide(parserFieldName)
                        .SetRighthandSide(
                            MethodCallBuilder.New()
                                .SetPrefix(SerializerResolverParamName + ".")
                                .SetDetermineStatement(false)
                                .SetMethodName(
                                    $"GetLeafValueParser<{valueParser.serializedType}, {valueParser.runtimeType}>"
                                )
                                .AddArgument($"\"{valueParser.graphQlTypeName}\"")
                        )
                );
            }

            AddBuildMethod(resultTypeDescriptor);

            AddBuildDataMethod(resultTypeDescriptor);

            AddRequiredDeserializeMethods(resultBuilderDescriptor.ResultType);

            return CodeFileBuilder.New()
                .SetNamespace(resultBuilderDescriptor.ResultType.Namespace)
                .AddType(ClassBuilder)
                .BuildAsync(writer);
        }

        /// <summary>
        /// Adds all required deserializers of the given type descriptors properties
        /// </summary>
        private void AddRequiredDeserializeMethods(TypeDescriptor typeDescriptor)
        {
            foreach (var property in typeDescriptor.Properties)
            {
                AddDeserializeMethod(property.Type);
            }
        }

        private void AddDeserializeMethod(ITypeDescriptor typeReference)
        {
            switch (typeReference)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    AddUpdateEntityArrayMethod(listTypeDescriptor);
                    break;
                case TypeDescriptor typeDescriptor:
                    switch (typeDescriptor.Kind)
                    {
                        case TypeKind.Scalar:
                            AddScalarTypeDeserializerMethod(typeDescriptor);
                            break;
                        case TypeKind.DataType:
                            AddDataTypeDeserializerMethod(typeDescriptor);
                            break;
                        case TypeKind.EntityType:
                            AddUpdateEntityMethod(typeDescriptor);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeReference));
            }
        }

        private void AddBuildMethod(TypeDescriptor resultType)
        {
            var responseParameterName = "response";
            var buildMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetName("Build")
                .SetReturnType(
                    TypeReferenceBuilder.New()
                        .SetName(WellKnownNames.IOperationResult)
                        .AddGeneric(resultType.Name)
                )
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(
                            TypeReferenceBuilder.New()
                                .SetName(WellKnownNames.Response)
                                .AddGeneric("JsonDocument")
                                .SetName(WellKnownNames.Response)
                        )
                        .SetName(responseParameterName)
                );

            buildMethod.AddCode(
                AssignmentBuilder.New()
                    .SetLefthandSide(
                        $"({resultType.Name} Result, {NamingConventions.ResultInfoNameFromTypeName(resultType.Name)} Info)? data"
                    )
                    .SetRighthandSide("null")
            );

            buildMethod.AddEmptyLine();
            buildMethod.AddCode(
                IfBuilder.New()
                    .SetCondition(
                        ConditionBuilder.New()
                            .Set("response.Body is not null")
                            .And("response.Body.RootElement.TryGetProperty(\"data\", out JsonElement obj)")
                    )
                    .AddCode("data = BuildData(obj);")
            );

            buildMethod.AddEmptyLine();
            buildMethod.AddCode(
                MethodCallBuilder.New()
                    .SetPrefix("return new ")
                    .SetMethodName($"{WellKnownNames.OperationResult}<{resultType.Name}>")
                    .AddArgument("data?.Result")
                    .AddArgument("data?.Info")
                    .AddArgument(ResultDataFactoryFieldName)
                    .AddArgument("null")
            );

            ClassBuilder.AddMethod(buildMethod);
        }

        private void AddScalarTypeDeserializerMethod(TypeDescriptor type)
        {
            var scalarDeserializer = MethodBuilder.New()
                .SetName(DeserializerMethodNameFromTypeName(type))
                .SetReturnType(type.Name)
                .AddParameter(ParameterBuilder.New().SetType("JsonElement").SetName(objParamName));

            scalarDeserializer.AddCode(
                EnsureJsonValueIsNotNull(),
                !type.IsNullable
            );

            scalarDeserializer.AddCode(
                $"return {type.Name.ToFieldName()}Parser.Parse({objParamName}.Get{type.Name.WithCapitalFirstChar()}()!);"
            );

            ClassBuilder.AddMethod(scalarDeserializer);
        }

        private CodeBlockBuilder EnsureJsonValueIsNotNull(string propertyName = objParamName)
        {
            return CodeBlockBuilder.New().AddCode(
                    IfBuilder.New()
                        .SetCondition(
                            ConditionBuilder.New()
                                .Set($"{propertyName} == null")
                        ).AddCode("throw new InvalidOperationException();")
                )
                .AddEmptyLine();
        }

        private MethodCallBuilder BuildUpdateMethodCall(NamedTypeReferenceDescriptor property)
        {
            var deserializeMethodCaller = MethodCallBuilder.New()
                .SetDetermineStatement(false)
                .SetMethodName(DeserializerMethodNameFromTypeName(property.Type));

            deserializeMethodCaller.AddArgument($"{objParamName}.GetPropertyOrNull(\"{property.Name.WithLowerFirstChar()}\")");

            if (!property.Type.IsScalarType)
            {
                deserializeMethodCaller.AddArgument(EntityIdsParam);
            }

            return deserializeMethodCaller;
        }

        private MethodCallBuilder BuildUpdateMethodCall(ITypeDescriptor property, string firstArg)
        {
            var deserializeMethodCaller = MethodCallBuilder.New()
                .SetDetermineStatement(false)
                .SetMethodName(DeserializerMethodNameFromTypeName(property));

            deserializeMethodCaller.AddArgument(firstArg);

            if (!property.IsScalarType)
            {
                deserializeMethodCaller.AddArgument(EntityIdsParam);
            }

            return deserializeMethodCaller;
        }
    }
}
