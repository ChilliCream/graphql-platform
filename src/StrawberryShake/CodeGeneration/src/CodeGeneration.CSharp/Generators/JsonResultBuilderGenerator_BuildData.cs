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
                        .SetLefthandSide($"using {TypeNames.IEntityUpdateSession} {_session}")
                        .SetRighthandSide(
                            MethodCallBuilder
                                .Inline()
                                .SetMethodName(_entityStore, "BeginUpdate")))
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLefthandSide($"var {_entityIds}")
                        .SetRighthandSide(MethodCallBuilder
                            .Inline()
                            .SetNew()
                            .SetMethodName(TypeNames.HashSet)
                            .AddGeneric(TypeNames.EntityId)))
                .AddEmptyLine();

            foreach (PropertyDescriptor property in
                resultNamedType.Properties.Where(prop => prop.Type.IsEntityType()))
            {
                buildDataMethod.AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLefthandSide(CodeBlockBuilder
                            .New()
                            .AddCode(property.Type.ToStateTypeReference())
                            .AddCode($"{GetParameterName(property.Name)}Id"))
                        .SetRighthandSide(BuildUpdateMethodCall(property)));
            }

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
                if (property.Type.IsEntityType())
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
                .AddArgument($"{_session}.{TypeNames.IEntityUpdateSession_Version}");
        }
    }
}
