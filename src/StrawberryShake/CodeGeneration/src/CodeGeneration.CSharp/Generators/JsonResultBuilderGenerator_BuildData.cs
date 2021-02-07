using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator : ClassBaseGenerator<ResultBuilderDescriptor>
    {
        private void AddBuildDataMethod(
            NamedTypeDescriptor resultNamedType,
            ClassBuilder classBuilder)
        {
            var objParameter = "obj";
            var buildDataMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Private)
                .SetName("BuildData")
                .SetReturnType(
                    $"({resultNamedType.Name}, " +
                    $"{ResultInfoNameFromTypeName(resultNamedType.ImplementedBy[0].Name)})")
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(TypeNames.JsonElement)
                        .SetName(objParameter));

            var sessionName = "session";
            buildDataMethod.AddCode(
                CodeLineBuilder.New()
                    .SetLine(
                        CodeBlockBuilder.New()
                            .AddCode(
                                $"using {TypeNames.IEntityUpdateSession} {sessionName} = ")
                            .AddCode(_entityStoreFieldName + ".BeginUpdate();")));

            var entityIdsName = "entityIds";
            buildDataMethod.AddCode(
                CodeLineBuilder.New()
                    .SetLine(
                        $"var {entityIdsName} = new {TypeNames.HashSet}<{TypeNames.EntityId}>();"));

            buildDataMethod.AddEmptyLine();
            foreach (PropertyDescriptor property in
                resultNamedType.Properties.Where(prop => prop.Type.IsEntityType()))
            {
                buildDataMethod.AddCode(
                    AssignmentBuilder.New()
                        .SetLefthandSide(CodeBlockBuilder.New()
                            .AddCode(property.Type.ToEntityIdBuilder())
                            .AddCode($"{property.Name.WithLowerFirstChar()}Id"))
                        .SetRighthandSide(BuildUpdateMethodCall(property, "")));
            }

            var resultInfoConstructor = MethodCallBuilder.New()
                .SetMethodName(
                    $"new {ResultInfoNameFromTypeName(resultNamedType.ImplementedBy[0].Name)}")
                .SetDetermineStatement(false);

            foreach (PropertyDescriptor property in resultNamedType.Properties)
            {
                if (property.Type.IsEntityType())
                {
                    resultInfoConstructor.AddArgument($"{property.Name.WithLowerFirstChar()}Id");
                }
                else
                {
                    resultInfoConstructor.AddArgument(BuildUpdateMethodCall(property, ""));
                }
            }

            resultInfoConstructor.AddArgument(entityIdsName);
            resultInfoConstructor.AddArgument(
                $"{sessionName}.{TypeNames.IEntityUpdateSession_Version}");

            buildDataMethod.AddEmptyLine();
            var resultInfoName = "resultInfo";
            buildDataMethod.AddCode(
                AssignmentBuilder.New()
                    .SetLefthandSide($"var {resultInfoName}")
                    .SetRighthandSide(resultInfoConstructor));

            buildDataMethod.AddEmptyLine();
            buildDataMethod.AddCode(
                $"return ({_resultDataFactoryFieldName}" +
                $".Create({resultInfoName}), {resultInfoName});");

            classBuilder.AddMethod(buildDataMethod);
        }
    }
}
