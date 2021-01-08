using System;
using System.Reflection.Emit;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
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

            return CodeFileBuilder.New()
                .SetNamespace(resultBuilderDescriptor.ResultType.Namespace)
                .AddType(ClassBuilder)
                .BuildAsync(writer);
        }

        private void AddBuildMethod(TypeDescriptor resultTypeDescriptor)
        {
            var responseParameterName = "response";
            var buildMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetName("Build")
                .SetReturnType(
                    TypeReferenceBuilder.New()
                        .SetName(WellKnownNames.IOperationResult)
                        .AddGeneric(resultTypeDescriptor.Name)
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
                        $"({resultTypeDescriptor.Name} Result, {NamingConventions.ResultInfoNameFromTypeName(resultTypeDescriptor.Name)} Info)? data"
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
                    .SetMethodName($"{WellKnownNames.OperationResult}<{resultTypeDescriptor.Name}>")
                    .AddArgument("data?.Result")
                    .AddArgument("data?.Info")
                    .AddArgument(ResultDataFactoryFieldName)
                    .AddArgument("null")
            );

            ClassBuilder.AddMethod(buildMethod);
        }

        private void AddBuildDataMethod(TypeDescriptor resultTypeDescriptor)
        {
            var buildDataMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetName("BuildData")
                .SetReturnType($"({resultTypeDescriptor.Name}, {NamingConventions.ResultInfoNameFromTypeName(resultTypeDescriptor.Name)})")
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType("JsonElement")
                        .SetName("obj")
                );

            buildDataMethod.AddCode(
                CodeLineBuilder.New()
                    .SetLine(
                        CodeBlockBuilder.New()
                            .AddCode($"using {WellKnownNames.IEntityUpdateSession} session = ")
                            .AddCode(EntityStoreFieldName + ".BeginUpdate();")
                    )
            );
            buildDataMethod.AddCode(CodeLineBuilder.New().SetLine($"var entityIds = new HashSet<{WellKnownNames.EntityId}>();"));

            buildDataMethod.AddEmptyLine();


            ClassBuilder.AddMethod(buildDataMethod);
        }
    }
}
