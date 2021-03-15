using System;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public partial class JsonResultBuilderGenerator
    {
        private const string _session = "session";
        private const string _resultInfo = "resultInfo";
        private const string _snapshot = "snapshot";

        private void AddBuildDataMethod(
            InterfaceTypeDescriptor resultNamedType,
            ClassBuilder classBuilder)
        {
            var concreteType =
                CreateResultInfoName(
                    resultNamedType.ImplementedBy.First().RuntimeType.Name);

            MethodBuilder buildDataMethod = classBuilder
                .AddMethod()
                .SetPrivate()
                .SetName("BuildData")
                .SetReturnType($"({resultNamedType.RuntimeType.Name}, {concreteType})")
                .AddParameter(_obj, x => x.SetType(TypeNames.JsonElement))
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLefthandSide($"var {_entityIds}")
                        .SetRighthandSide(MethodCallBuilder
                            .Inline()
                            .SetNew()
                            .SetMethodName(TypeNames.HashSet)
                            .AddGeneric(TypeNames.EntityId)))
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLefthandSide($"{TypeNames.IEntityStoreSnapshot} {_snapshot}")
                        .SetRighthandSide("default!"))
                .AddEmptyLine();


            CodeBlockBuilder storeUpdateBody = CodeBlockBuilder.New();

            foreach (PropertyDescriptor property in
                resultNamedType.Properties.Where(prop => prop.Type.IsOrContainsEntityType()))
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
                    .SetMethodName(_entityStore, "Update")
                    .AddArgument(LambdaBuilder
                        .New()
                        .AddArgument(_session)
                        .SetBlock(true)
                        .SetCode(storeUpdateBody)));

            buildDataMethod
                .AddEmptyLine()
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLefthandSide($"var {_resultInfo}")
                        .SetRighthandSide(
                            CreateResultInfoMethodCall(resultNamedType, concreteType)))
                .AddEmptyLine()
                .AddCode(
                    TupleBuilder
                        .Inline()
                        .SetDetermineStatement(true)
                        .SetReturn()
                        .AddMember(MethodCallBuilder
                            .Inline()
                            .SetMethodName(_resultDataFactory, "Create")
                            .AddArgument(_resultInfo))
                        .AddMember(_resultInfo));
        }

        private MethodCallBuilder CreateResultInfoMethodCall(
            InterfaceTypeDescriptor resultNamedType,
            string concreteType)
        {
            MethodCallBuilder resultInfoConstructor = MethodCallBuilder
                .Inline()
                .SetMethodName($"new {concreteType}");

            foreach (PropertyDescriptor property in resultNamedType.Properties)
            {
                if (property.Type.IsOrContainsEntityType())
                {
                    resultInfoConstructor.AddArgument($"{GetParameterName(property.Name)}Id");
                }
                else
                {
                    resultInfoConstructor.AddArgument(BuildUpdateMethodCall(property));
                }
            }

            return resultInfoConstructor
                .AddArgument(_entityIds)
                .AddArgument($"{_snapshot}.Version");
        }
    }
}
