using System;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using FieldBuilder = StrawberryShake.CodeGeneration.CSharp.Builders.FieldBuilder;
using MethodBuilder = StrawberryShake.CodeGeneration.CSharp.Builders.MethodBuilder;
using ParameterBuilder = StrawberryShake.CodeGeneration.CSharp.Builders.ParameterBuilder;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class JsonResultBuilderGenerator : ClassBaseGenerator<ResultBuilderDescriptor>
    {
        private const string EntityStoreFieldName = "_entityStore";
        private const string ExtractIdFieldName = "_extractId";
        private const string ResultDataFactoryFieldName = "_resultDataFactory";
        private const string SerializerResolverParamName = "serializerResolver";
        private const string TransportResultRootTypeName = "JsonElement";

        private string GetUpdateMethodName(NamedTypeReferenceDescriptor namedTypeReference) =>
            $"Update{namedTypeReference.TypeName}Entity";

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
                        AddUpdateMethod(property);
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
            foreach (NamedTypeReferenceDescriptor typePropertyDescriptor in resultType.Properties.Where(
                prop => prop.IsEntityType
            ))
            {
                buildDataMethod.AddCode(
                    AssignmentBuilder.New()
                        .SetLefthandSide($"{WellKnownNames.EntityId} {typePropertyDescriptor.Name}Id")
                        .SetRighthandSide(
                            MethodCallBuilder.New()
                                .SetDetermineStatement(false)
                                .SetMethodName(GetUpdateMethodName(typePropertyDescriptor))
                                .AddArgument(
                                    $"{objParameter}.GetProperty(\"{typePropertyDescriptor.Name.WithLowerFirstChar()}\")"
                                )
                                .AddArgument(entityIdsName)
                        )
                );
            }


            var resultInfoConstructor = MethodCallBuilder.New()
                .SetMethodName($"new {NamingConventions.ResultInfoNameFromTypeName(resultType.Name)}")
                .SetDetermineStatement(false);

            foreach (NamedTypeReferenceDescriptor typePropertyDescriptor in resultType.Properties)
            {
                if (typePropertyDescriptor.IsEntityType)
                {
                    resultInfoConstructor.AddArgument($"{typePropertyDescriptor.Name}Id");
                }
                else
                {
                    resultInfoConstructor.AddArgument(
                        MethodCallBuilder.New()
                            .SetDetermineStatement(false)
                            .SetMethodName(
                                NamingConventions.TypeDeserializeMethodNameFromTypeName(typePropertyDescriptor)
                            )
                            .AddArgument(objParameter)
                            .AddArgument($"\"{typePropertyDescriptor.Name.WithLowerFirstChar()}\"")
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

        private void AddUpdateMethod(NamedTypeReferenceDescriptor namedTypeReference)
        {
            var objParamName = "obj";
            var entityIdsParamName = "entityIds";
            var updateEntityMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Private)
                .SetName(NamingConventions.TypeUpdateMethodNameFromTypeName(namedTypeReference))
                .SetReturnType(WellKnownNames.EntityId)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType("JsonElement")
                        .SetName(objParamName)
                )
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType($"ISet<{WellKnownNames.EntityId}>")
                        .SetName(entityIdsParamName)
                );

            var entityIdVarName = "entityId";
            updateEntityMethod.AddCode(
                $"{WellKnownNames.EntityId} {entityIdVarName} = {ExtractIdFieldName}({objParamName});"
            );
            updateEntityMethod.AddCode($"{entityIdsParamName}.Add({entityIdVarName});");

            foreach (TypeDescriptor concreteType in namedTypeReference.Type.IsImplementedBy)
            {
                updateEntityMethod.AddEmptyLine();
                var ifStatement = IfBuilder.New()
                    .SetCondition($"entityId.Name.Equals(\"{concreteType.Name}\", StringComparison.Ordinal)");

                var entityTypeName = NamingConventions.EntityTypeNameFromTypeName(concreteType.Name);

                var entityVarName = "entity";
                ifStatement.AddCode(
                    $"{entityTypeName} {entityTypeName} = {EntityStoreFieldName}.GetOrCreate<{entityTypeName}>({entityIdVarName});"
                );
                foreach (NamedTypeReferenceDescriptor property in concreteType.Properties.Where(
                    prop => !prop.IsEntityType
                ))
                {
                    var passEntityIdsCode = property.Type.Kind == TypeKind.DataType ? $", {entityIdsParamName}" : "";
                    ifStatement.AddCode(
                        AssignmentBuilder.New()
                            .SetLefthandSide($"{entityVarName}.{property.Name}")
                            .SetRighthandSide(
                                $"{NamingConventions.TypeDeserializeMethodNameFromTypeName(property)}({objParamName}{passEntityIdsCode}, \"{property.Name.WithLowerFirstChar()}\")"
                            )
                    );
                }

                ifStatement.AddEmptyLine();
                ifStatement.AddCode($"return {entityIdVarName};");
                updateEntityMethod.AddCode(ifStatement);
            }

            updateEntityMethod.AddEmptyLine();
            updateEntityMethod.AddCode("throw new NotSupportedException();");

            ClassBuilder.AddMethod(updateEntityMethod);
            AddRequiredDeserializeMethods(namedTypeReference.Type);
        }

        private void AddDataTypeDeserializerMethod(TypeReferenceDescriptor type)
        {
            var objParamName = "obj";
            var entityIds = "entityIds";
            var propertyNameParamName = "propertyName";

            var dateDeserializer = MethodBuilder.New()
                .SetName(NamingConventions.TypeDeserializeMethodNameFromTypeName(type))
                .AddParameter(ParameterBuilder.New().SetType("JsonElement").SetName(objParamName))
                .AddParameter(ParameterBuilder.New().SetType($"ISet<{WellKnownNames.EntityId}>").SetName(entityIds))
                .AddParameter(ParameterBuilder.New().SetType("string").SetName(propertyNameParamName));

            // dateDeserializer.AddCode(
            //     IfBuilder.New()
            //         .SetCondition(ConditionBuilder.New()
            //             .Set($"{entityIds}.TryGetProperty({propertyNameParamName}, out JsonElement property)")
            //             .And("property.ValueKind != JsonValueKind.Null", !type.IsNullable))
            //         .AddCode($"return {type.TypeName.ToFieldName()}Parser.Parse(property.Get{type.TypeName.WithCapitalFirstChar()}()!);")
            // );

            dateDeserializer.AddEmptyLine();
            dateDeserializer.AddCode("throw new InvalidOperationException();");

            ClassBuilder.AddMethod(dateDeserializer);
            AddRequiredDeserializeMethods(type.Type);
        }

        private void AddScalarTypeDeserializerMethod(TypeReferenceDescriptor type)
        {
            var objParamName = "obj";
            var propertyNameParamName = "propertyName";

            var scalarDeserializer = MethodBuilder.New()
                .SetName(NamingConventions.TypeDeserializeMethodNameFromTypeName(type))
                .AddParameter(ParameterBuilder.New().SetType("JsonElement").SetName(objParamName))
                .AddParameter(ParameterBuilder.New().SetType("string").SetName(propertyNameParamName));

            scalarDeserializer.AddCode(
                IfBuilder.New()
                    .SetCondition(ConditionBuilder.New()
                        .Set($"{objParamName}.TryGetProperty({propertyNameParamName}, out JsonElement property)")
                        .And("property.ValueKind != JsonValueKind.Null", !type.IsNullable))
                    .AddCode($"return {type.TypeName.ToFieldName()}Parser.Parse(property.Get{type.TypeName.WithCapitalFirstChar()}()!);")
            );

            scalarDeserializer.AddEmptyLine();
            scalarDeserializer.AddCode("throw new InvalidOperationException();");

            ClassBuilder.AddMethod(scalarDeserializer);
        }
    }
}
