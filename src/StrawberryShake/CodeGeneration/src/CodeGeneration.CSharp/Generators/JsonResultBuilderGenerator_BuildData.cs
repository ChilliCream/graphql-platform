using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class JsonResultBuilderGenerator
{
    private const string _session = "session";
    private const string _snapshot = "snapshot";

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
            .AddParameter(_obj, x => x.SetType(TypeNames.JsonElement));

        // var entityIds = new HashSet<EntityId>();
        // var pathToEntityId = new Dictionary<string, EntityId>();
        if (settings.IsStoreEnabled())
        {
            buildDataMethod
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLefthandSide($"var {GetParameterName(_entityIds)}")
                        .SetRighthandSide(MethodCallBuilder
                            .Inline()
                            .SetNew()
                            .SetMethodName(TypeNames.HashSet)
                            .AddGeneric(TypeNames.EntityId)))
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLefthandSide($"{TypeNames.IEntityStoreSnapshot} {_snapshot}")
                        .SetRighthandSide("default!"));
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
                        .SetLefthandSide(CodeBlockBuilder
                            .New()
                            .AddCode(property.Type.ToStateTypeReference())
                            .AddCode(variableName))
                        .SetRighthandSide("default!"));

                storeUpdateBody
                    .AddCode(AssignmentBuilder
                        .New()
                        .SetLefthandSide(variableName)
                        .SetRighthandSide(BuildUpdateMethodCall(property)));
            }

            storeUpdateBody
                .AddEmptyLine()
                .AddCode(AssignmentBuilder
                    .New()
                    .SetLefthandSide(_snapshot)
                    .SetRighthandSide($"{_session}.CurrentSnapshot"));

            buildDataMethod
                .AddCode(MethodCallBuilder
                    .New()
                    .SetMethodName(GetFieldName(_entityStore), "Update")
                    .AddArgument(LambdaBuilder
                        .New()
                        .AddArgument(_session)
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
                .AddArgument(_entityIds)
                .AddArgument($"{_snapshot}.Version");
        }

        return resultInfoConstructor;
    }
}