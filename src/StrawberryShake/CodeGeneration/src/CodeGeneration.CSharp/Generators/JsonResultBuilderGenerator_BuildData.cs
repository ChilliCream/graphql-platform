using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class JsonResultBuilderGenerator
{
    private const string Session = "session";
    private const string Snapshot = "snapshot";

    private void AddBuildDataMethod(
        CSharpSyntaxGeneratorSettings settings,
        InterfaceTypeDescriptor resultNamedType,
        ClassBuilder classBuilder)
    {
        var concreteType =
            CreateResultInfoName(
                resultNamedType.ImplementedBy.First().RuntimeType.Name);

        // protected override IOperationResultDataInfo BuildData(JsonElement dataProp)
        var buildDataMethod = classBuilder
            .AddMethod()
            .SetProtected()
            .SetOverride()
            .SetName("BuildData")
            .SetReturnType(TypeNames.IOperationResultDataInfo)
            .AddParameter(Obj, x => x.SetType(TypeNames.JsonElement));

        // var entityIds = new HashSet<EntityId>();
        // var pathToEntityId = new Dictionary<string, EntityId>();
        if (settings.IsStoreEnabled())
        {
            buildDataMethod
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"var {GetParameterName(EntityIds)}")
                        .SetRightHandSide(MethodCallBuilder
                            .Inline()
                            .SetNew()
                            .SetMethodName(TypeNames.HashSet)
                            .AddGeneric(TypeNames.EntityId)))
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"{TypeNames.IEntityStoreSnapshot} {Snapshot}")
                        .SetRightHandSide("default!"));
        }

        buildDataMethod.AddEmptyLine();

        var storeUpdateBody = CodeBlockBuilder.New();

        if (settings.IsStoreEnabled())
        {
            foreach (var property in
                     resultNamedType.Properties.Where(prop => prop.Type.IsOrContainsEntity()))
            {
                var variableName = $"{GetParameterName(property.Name)}Id";

                buildDataMethod
                    .AddCode(AssignmentBuilder
                        .New()
                        .SetLeftHandSide(CodeBlockBuilder
                            .New()
                            .AddCode(property.Type.ToStateTypeReference())
                            .AddCode(variableName))
                        .SetRightHandSide("default!"));

                storeUpdateBody
                    .AddCode(AssignmentBuilder
                        .New()
                        .SetLeftHandSide(variableName)
                        .SetRightHandSide(BuildUpdateMethodCall(property)));
            }

            storeUpdateBody
                .AddEmptyLine()
                .AddCode(AssignmentBuilder
                    .New()
                    .SetLeftHandSide(Snapshot)
                    .SetRightHandSide($"{Session}.CurrentSnapshot"));

            buildDataMethod
                .AddCode(MethodCallBuilder
                    .New()
                    .SetMethodName(GetFieldName(EntityStore), "Update")
                    .AddArgument(LambdaBuilder
                        .New()
                        .AddArgument(Session)
                        .SetBlock(true)
                        .SetCode(storeUpdateBody)));
        }

        buildDataMethod
            .AddEmptyLine()
            .AddCode(
                CreateResultInfoMethodCall(
                        settings,
                        resultNamedType,
                        concreteType)
                    .SetDetermineStatement(true)
                    .SetReturn());
    }

    private MethodCallBuilder CreateResultInfoMethodCall(
        CSharpSyntaxGeneratorSettings settings,
        InterfaceTypeDescriptor resultNamedType,
        string concreteType)
    {
        var resultInfoConstructor = MethodCallBuilder
            .Inline()
            .SetMethodName($"new {concreteType}");

        foreach (var property in resultNamedType.Properties)
        {
            if (property.Type.IsOrContainsEntity())
            {
                resultInfoConstructor.AddArgument($"{GetParameterName(property.Name)}Id");
            }
            else
            {
                resultInfoConstructor.AddArgument(BuildUpdateMethodCall(property));
            }
        }

        if (settings.IsStoreEnabled())
        {
            resultInfoConstructor
                .AddArgument(EntityIds)
                .AddArgument($"{Snapshot}.Version");
        }

        return resultInfoConstructor;
    }
}
