using System.Collections.Generic;
using System.Linq;
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
                            .SetMethodName(_idSerializer, "Parse")
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
                    methodBuilder
                        .AddEmptyLine()
                        .AddCode(CreateUpdateEntityStatement(concreteType)
                            .AddCode($"return {_entityId};"));
                }

                methodBuilder.AddEmptyLine();
                methodBuilder.AddCode(ExceptionBuilder.New(TypeNames.NotSupportedException));
            }
            else if (namedTypeDescriptor is ObjectTypeDescriptor objectTypeDescriptor)
            {
                BuildTryGetEntityIf(
                        CreateEntityType(
                            objectTypeDescriptor.Name,
                            objectTypeDescriptor.RuntimeType.NamespaceWithoutGlobal))
                    .AddCode(CreateEntityConstructorCall(objectTypeDescriptor, false))
                    .AddElse(CreateEntityConstructorCall(objectTypeDescriptor, true));

                methodBuilder.AddEmptyLine();
                methodBuilder.AddCode($"return {_entityId};");
            }

            AddRequiredDeserializeMethods(namedTypeDescriptor, classBuilder, processed);
        }

        private IfBuilder CreateUpdateEntityStatement(
            ObjectTypeDescriptor concreteType)
        {
            IfBuilder ifStatement = IfBuilder
                .New()
                .SetCondition(
                    MethodCallBuilder
                        .Inline()
                        .SetMethodName(_entityId, "Name", nameof(string.Equals))
                        .AddArgument(concreteType.Name.AsStringToken())
                        .AddArgument(TypeNames.OrdinalStringComparison));

            RuntimeTypeInfo entityTypeName = CreateEntityType(
                concreteType.Name,
                concreteType.RuntimeType.NamespaceWithoutGlobal);

            IfBuilder ifBuilder = BuildTryGetEntityIf(entityTypeName)
                .AddCode(CreateEntityConstructorCall(concreteType, false))
                .AddElse(CreateEntityConstructorCall(concreteType, true));

            return ifStatement
                .AddCode(ifBuilder)
                .AddEmptyLine();
        }

        private ICode CreateEntityConstructorCall(
            ObjectTypeDescriptor objectTypeDescriptor,
            bool assignDefault)
        {
            var propertyLookup = objectTypeDescriptor.Properties.ToDictionary(x => x.Name.Value);

            MethodCallBuilder newEntity = MethodCallBuilder
                .Inline()
                .SetNew()
                .SetMethodName(objectTypeDescriptor.EntityTypeDescriptor.RuntimeType.ToString());

            foreach (PropertyDescriptor property in
                objectTypeDescriptor.EntityTypeDescriptor.Properties.Values)
            {
                if (propertyLookup.TryGetValue(property.Name.Value, out var ownProperty))
                {
                    newEntity.AddArgument(BuildUpdateMethodCall(ownProperty));
                }
                else if (assignDefault)
                {
                    newEntity.AddArgument("default!");
                }
                else
                {
                    newEntity.AddArgument($"{_entity}.{property.Name}");
                }
            }

            return MethodCallBuilder
                .New()
                .SetMethodName(_session, "SetEntity")
                .AddArgument(_entityId)
                .AddArgument(newEntity);
        }

        private IfBuilder BuildTryGetEntityIf(RuntimeTypeInfo entityType)
        {
            return IfBuilder
                .New()
                .SetCondition(MethodCallBuilder
                    .Inline()
                    .SetMethodName(_session, "CurrentSnapshot", "TryGetEntity")
                    .AddArgument(_entityId)
                    .AddOutArgument(_entity, entityType.ToString()));
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
