using System;
using System.Linq;
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
        private const string PropertyNameParam = "propertyName";
        private const string objParamName = "obj";


        private string GetUpdateMethodName(NamedTypeReferenceDescriptor namedTypeReference) =>
            $"Update{namedTypeReference.TypeName}Entity";

        private string TypeDeserializeMethodNameFromTypeName(TypeReferenceDescriptor namedTypeReferenceDescriptor)
        {
            var ret = "Deserialize";
            ret += namedTypeReferenceDescriptor.IsNullable ? "Nullable" : "NonNullable";
            ret += namedTypeReferenceDescriptor.Type.Name.WithCapitalFirstChar();
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
                        .AssertNonNull()
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

        private void AddRequiredDeserializeMethods(TypeDescriptor typeDescriptor)
        {
            foreach (var property in typeDescriptor.Properties
            )
            {
                switch (property.Type.Kind)
                {
                    case TypeKind.Scalar:
                        AddScalarTypeDeserializerMethod(property);
                        break;
                    case TypeKind.DataType:
                        AddDataTypeDeserializerMethod(property);
                        break;
                    case TypeKind.EntityType:
                        AddUpdateEntityMethod(property);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
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

        private void AddBuildDataMethod(TypeDescriptor resultType)
        {
            var objParameter = "obj";
            var buildDataMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Private)
                .SetName("BuildData")
                .SetReturnType($"({resultType.Name}, {NamingConventions.ResultInfoNameFromTypeName(resultType.Name)})")
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType("JsonElement")
                        .SetName(objParameter)
                );

            var sessionName = "session";
            buildDataMethod.AddCode(
                CodeLineBuilder.New()
                    .SetLine(
                        CodeBlockBuilder.New()
                            .AddCode($"using {WellKnownNames.IEntityUpdateSession} {sessionName} = ")
                            .AddCode(EntityStoreFieldName + ".BeginUpdate();")
                    )
            );
            var entityIdsName = "entityIds";
            buildDataMethod.AddCode(
                CodeLineBuilder.New().SetLine($"var {entityIdsName} = new HashSet<{WellKnownNames.EntityId}>();")
            );

            buildDataMethod.AddEmptyLine();
            foreach (NamedTypeReferenceDescriptor property in resultType.Properties.Where(prop => prop.IsEntityType))
            {
                buildDataMethod.AddCode(
                    AssignmentBuilder.New()
                        .SetLefthandSide($"{WellKnownNames.EntityId} {property.Name}Id")
                        .SetRighthandSide(
                            BuildUpdateMethodCall(
                                property,
                                objParameter
                            )
                        )
                );
            }


            var resultInfoConstructor = MethodCallBuilder.New()
                .SetMethodName($"new {NamingConventions.ResultInfoNameFromTypeName(resultType.Name)}")
                .SetDetermineStatement(false);

            foreach (NamedTypeReferenceDescriptor property in resultType.Properties)
            {
                if (property.IsEntityType)
                {
                    resultInfoConstructor.AddArgument($"{property.Name}Id");
                }
                else
                {
                    resultInfoConstructor.AddArgument(
                        BuildUpdateMethodCall(
                            property,
                            objParameter
                        )
                    );
                }
            }

            resultInfoConstructor.AddArgument(entityIdsName);
            resultInfoConstructor.AddArgument($"{sessionName}.{WellKnownNames.IEntityUpdateSession_Version}");

            buildDataMethod.AddEmptyLine();
            var resultInfoName = "resultInfo";
            buildDataMethod.AddCode(
                AssignmentBuilder.New()
                    .SetLefthandSide($"var {resultInfoName}")
                    .SetRighthandSide(resultInfoConstructor)
            );

            buildDataMethod.AddEmptyLine();
            buildDataMethod.AddCode(
                $"return ({ResultDataFactoryFieldName}.Create({resultInfoName}), {resultInfoName});"
            );

            ClassBuilder.AddMethod(buildDataMethod);
        }


        private void AddScalarTypeDeserializerMethod(TypeReferenceDescriptor type)
        {
            var scalarDeserializer = MethodBuilder.New()
                .SetName(TypeDeserializeMethodNameFromTypeName(type))
                .SetReturnType(type.TypeName)
                .AddParameter(ParameterBuilder.New().SetType("JsonElement").SetName(objParamName));

            scalarDeserializer.AddCode(
                EnsureJsonValueIsNotNull(),
                !type.IsNullable
            );

            scalarDeserializer.AddCode(
                $"return {type.TypeName.ToFieldName()}Parser.Parse({objParamName}.Get{type.TypeName.WithCapitalFirstChar()}()!);"
            );

            ClassBuilder.AddMethod(scalarDeserializer);
        }

        private CodeBlockBuilder EnsureJsonValueIsNotNull()
        {
            return CodeBlockBuilder.New().AddCode(
                    IfBuilder.New()
                        .SetCondition(
                            ConditionBuilder.New()
                                .Set($"{objParamName}.ValueKind == JsonValueKind.Null")
                        ).AddCode("throw new InvalidOperationException();")
                )
                .AddEmptyLine();
        }

        private MethodCallBuilder BuildUpdateMethodCall(NamedTypeReferenceDescriptor property, string firstArg)
        {
            var deserializeMethodCaller = MethodCallBuilder.New()
                .SetDetermineStatement(false)
                .SetMethodName(
                    property.IsEntityType
                        ? GetUpdateMethodName(property)
                        : TypeDeserializeMethodNameFromTypeName(property)
                );

            deserializeMethodCaller.AddArgument(firstArg);

            if (property.IsEntityType || property.IsDataType)
            {
                deserializeMethodCaller.AddArgument(EntityIdsParam);
            }

            return deserializeMethodCaller;
        }
    }
}
