using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public partial class JsonResultBuilderGenerator
    {
        private const string _entityId = "entityId";
        private const string _entity = "entity";

        private void AddUpdateEntityMethod(
            ClassBuilder classBuilder,
            MethodBuilder methodBuilder,
            INamedTypeDescriptor namedTypeDescriptor,
            HashSet<string> processed)
        {
            methodBuilder.AddCode(
                AssignmentBuilder
                    .New()
                    .SetLefthandSide($"{TypeNames.EntityId} {_entityId}")
                    .SetRighthandSide(
                        MethodCallBuilder
                            .Inline()
                            .SetMethodName(_extractId)
                            .AddArgument($"{_obj}.Value")));

            methodBuilder.AddCode(
                MethodCallBuilder
                    .New()
                    .SetMethodName(_entityIds, nameof(List<object>.Add))
                    .AddArgument(_entityId));

            methodBuilder.AddEmptyLine();

            if (namedTypeDescriptor is InterfaceTypeDescriptor interfaceTypeDescriptor)
            {
                // If the type is an interface
                foreach (ObjectTypeDescriptor concreteType in interfaceTypeDescriptor.ImplementedBy)
                {
                    IfBuilder ifStatement = IfBuilder
                        .New()
                        .SetCondition(
                            MethodCallBuilder
                                .Inline()
                                .SetMethodName(_entityId, "Name", nameof(string.Equals))
                                .AddArgument(concreteType.Name.AsStringToken())
                                .AddArgument(TypeNames.OrdinalStringComparison));

                    var entityTypeName = CreateEntityTypeName(concreteType.Name);

                    WriteEntityLoader(
                        ifStatement,
                        entityTypeName);

                    WritePropertyAssignments(
                        ifStatement,
                        concreteType.Properties);

                    ifStatement
                        .AddEmptyLine()
                        .AddCode($"return {_entityId};");

                    methodBuilder
                        .AddEmptyLine()
                        .AddCode(ifStatement);
                }

                methodBuilder.AddEmptyLine();
                methodBuilder.AddCode(ExceptionBuilder.New(TypeNames.NotSupportedException));
            }
            else if (namedTypeDescriptor is ComplexTypeDescriptor complexTypeDescriptor)
            {
                WriteEntityLoader(methodBuilder, CreateEntityTypeName(namedTypeDescriptor.Name));
                WritePropertyAssignments(methodBuilder, complexTypeDescriptor.Properties);

                methodBuilder.AddEmptyLine();
                methodBuilder.AddCode($"return {_entityId};");
            }

            AddRequiredDeserializeMethods(namedTypeDescriptor, classBuilder, processed);
        }

        private void WriteEntityLoader<T>(
            ICodeContainer<T> codeContainer,
            string entityTypeName)
        {
            codeContainer.AddCode(
                AssignmentBuilder
                    .New()
                    .SetLefthandSide($"{entityTypeName} {_entity}")
                    .SetRighthandSide(
                        MethodCallBuilder
                            .Inline()
                            .SetMethodName(_entityStore, "GetOrCreate")
                            .AddGeneric(entityTypeName)
                            .AddArgument(_entityId)));
        }

        private void WritePropertyAssignments<T>(
            ICodeContainer<T> codeContainer,
            IReadOnlyList<PropertyDescriptor> properties)
        {
            foreach (PropertyDescriptor property in properties)
            {
                codeContainer.AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLefthandSide($"{_entity}.{property.Name}")
                        .SetRighthandSide(BuildUpdateMethodCall(property)));
            }
        }
    }
}
