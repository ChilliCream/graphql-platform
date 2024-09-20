using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

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
                .SetLeftHandSide($"{TypeNames.EntityId} {_entityId}")
                .SetRightHandSide(
                    MethodCallBuilder
                        .Inline()
                        .SetMethodName(GetFieldName(_idSerializer), "Parse")
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
            foreach (var concreteType in interfaceTypeDescriptor.ImplementedBy)
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
        var ifStatement = IfBuilder
            .New()
            .SetCondition(
                MethodCallBuilder
                    .Inline()
                    .SetMethodName(_entityId, "Name", nameof(string.Equals))
                    .AddArgument(concreteType.Name.AsStringToken())
                    .AddArgument(TypeNames.OrdinalStringComparison));

        var entityTypeName = CreateEntityType(
            concreteType.Name,
            concreteType.RuntimeType.NamespaceWithoutGlobal);

        var ifBuilder = BuildTryGetEntityIf(entityTypeName)
            .AddCode(CreateEntityConstructorCall(concreteType, false))
            .AddElse(CreateEntityConstructorCall(concreteType, true));

        return ifStatement
            .AddCode(ifBuilder)
            .AddEmptyLine();
    }

    private static ICode CreateEntityConstructorCall(
        ObjectTypeDescriptor objectType,
        bool assignDefault)
    {
        var propertyLookup = objectType.Properties.ToDictionary(x => x.Name);
        var fragments = objectType.Deferred.ToDictionary(t => t.FragmentIndicator);

        // include properties from fragments
        foreach (var fragment in fragments.Values)
        {
            foreach (var property in fragment.Class.Properties)
            {
                if (!propertyLookup.ContainsKey(property.Name))
                {
                    propertyLookup.Add(property.Name, EnsureDeferredFieldIsNullable(property));
                }
            }
        }

        var newEntity = MethodCallBuilder
            .Inline()
            .SetNew()
            .SetMethodName(objectType.EntityTypeDescriptor.RuntimeType.ToString());

        foreach (var property in
            objectType.EntityTypeDescriptor.Properties.Values)
        {
            if (propertyLookup.TryGetValue(property.Name, out var prop))
            {
                newEntity.AddArgument(BuildUpdateMethodCall(prop));
            }
            else if (fragments.TryGetValue(property.Name, out var frag))
            {
                newEntity.AddArgument(BuildFragmentMethodCall(frag));
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

    private static IfBuilder BuildTryGetEntityIf(RuntimeTypeInfo entityType)
    {
        return IfBuilder
            .New()
            .SetCondition(MethodCallBuilder
                .Inline()
                .SetMethodName(_session, "CurrentSnapshot", "TryGetEntity")
                .AddArgument(_entityId)
                .AddOutArgument(_entity, entityType.ToString()));
    }

    private static PropertyDescriptor EnsureDeferredFieldIsNullable(PropertyDescriptor property)
    {
        if (property.Type.IsNonNull())
        {
            property = new PropertyDescriptor(
                property.Name,
                property.FieldName,
                property.Type.InnerType(),
                property.Description,
                PropertyKind.DeferredField);
        }

        return property;
    }
}
