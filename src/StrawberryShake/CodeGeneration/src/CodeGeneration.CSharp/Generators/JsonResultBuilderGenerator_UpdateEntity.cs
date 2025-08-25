using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class JsonResultBuilderGenerator
{
    private const string EntityId = "entityId";
    private const string Entity = "entity";

    private void AddUpdateEntityMethod(
        ClassBuilder classBuilder,
        MethodBuilder methodBuilder,
        INamedTypeDescriptor namedTypeDescriptor,
        HashSet<string> processed)
    {
        methodBuilder.AddCode(
            AssignmentBuilder
                .New()
                .SetLeftHandSide($"{TypeNames.EntityId} {EntityId}")
                .SetRightHandSide(
                    MethodCallBuilder
                        .Inline()
                        .SetMethodName(GetFieldName(IdSerializer), "Parse")
                        .AddArgument($"{Obj}.Value")));

        methodBuilder.AddCode(
            MethodCallBuilder
                .New()
                .SetMethodName(EntityIds, nameof(List<object>.Add))
                .AddArgument(EntityId));

        methodBuilder.AddEmptyLine();

        if (namedTypeDescriptor is InterfaceTypeDescriptor interfaceTypeDescriptor)
        {
            // If the type is an interface
            foreach (var concreteType in interfaceTypeDescriptor.ImplementedBy)
            {
                methodBuilder
                    .AddEmptyLine()
                    .AddCode(CreateUpdateEntityStatement(concreteType)
                        .AddCode($"return {EntityId};"));
            }

            methodBuilder.AddEmptyLine();
            methodBuilder.AddCode(ExceptionBuilder.New(TypeNames.NotSupportedException));
        }
        else if (namedTypeDescriptor is ObjectTypeDescriptor objectTypeDescriptor)
        {
            methodBuilder.AddCode(
                BuildTryGetEntityIf(
                    CreateEntityType(
                            objectTypeDescriptor.Name,
                            objectTypeDescriptor.RuntimeType.NamespaceWithoutGlobal))
                    .AddCode(CreateEntityConstructorCall(objectTypeDescriptor, false))
                    .AddElse(CreateEntityConstructorCall(objectTypeDescriptor, true)));

            methodBuilder.AddEmptyLine();
            methodBuilder.AddCode($"return {EntityId};");
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
                    .SetMethodName(EntityId, "Name", nameof(string.Equals))
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

        var codeBlock = GenerateArgumentsFromResponse(objectType, propertyLookup, fragments);
        if (assignDefault)
        {
            // Merge: Check whether the same entity was already stored while evaluating an argument
            // An entity may reference itself but with a different selection set.
            codeBlock.AddCode(
                BuildTryGetEntityIf(null)
                .AddCode(CreateSetEntityMethodCall(objectType, false, propertyLookup, fragments))
                .AddElse(CreateSetEntityMethodCall(objectType, true, propertyLookup, fragments)));
        }
        else
        {
            codeBlock.AddCode(
                CreateSetEntityMethodCall(objectType, assignDefault, propertyLookup, fragments));
        }

        return codeBlock;
    }

    private static CodeBlockBuilder GenerateArgumentsFromResponse(
        ObjectTypeDescriptor objectType,
        Dictionary<string, PropertyDescriptor> propertyLookup,
        Dictionary<string, DeferredFragmentDescriptor> fragments)
    {
        var codeBlockBuilder = CodeBlockBuilder.New();
        var argumentIndex = 0;
        foreach (var property in
            objectType.EntityTypeDescriptor.Properties.Values)
        {
            if (propertyLookup.TryGetValue(property.Name, out var prop))
            {
                codeBlockBuilder.AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"var arg{argumentIndex++}")
                        .SetRightHandSide(BuildUpdateMethodCall(prop)));
            }
            else if (fragments.TryGetValue(property.Name, out var frag))
            {
                codeBlockBuilder.AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"var arg{argumentIndex++}")
                        .SetRightHandSide(BuildFragmentMethodCall(frag)));
            }
        }

        return codeBlockBuilder;
    }

    private static MethodCallBuilder CreateSetEntityMethodCall(
        ObjectTypeDescriptor objectType,
        bool assignDefault,
        Dictionary<string, PropertyDescriptor> propertyLookup,
        Dictionary<string, DeferredFragmentDescriptor> fragments)
    {
        var newEntity = MethodCallBuilder
            .Inline()
            .SetNew()
            .SetMethodName(objectType.EntityTypeDescriptor.RuntimeType.ToString());

        var argumentIndex = 0;
        foreach (var property in
            objectType.EntityTypeDescriptor.Properties.Values)
        {
            if (propertyLookup.ContainsKey(property.Name)
                || fragments.ContainsKey(property.Name))
            {
                newEntity.AddArgument($"arg{argumentIndex++}");
            }
            else if (assignDefault)
            {
                newEntity.AddArgument("default!");
            }
            else
            {
                newEntity.AddArgument($"{Entity}.{property.Name}");
            }
        }

        return MethodCallBuilder
            .New()
            .SetMethodName(Session, "SetEntity")
            .AddArgument(EntityId)
            .AddArgument(newEntity);
    }

    private static IfBuilder BuildTryGetEntityIf(RuntimeTypeInfo? entityType)
    {
        return IfBuilder
            .New()
            .SetCondition(MethodCallBuilder
                .Inline()
                .SetMethodName(Session, "CurrentSnapshot", "TryGetEntity")
                .AddArgument(EntityId)
                .AddOutArgument(Entity, entityType?.ToString()));
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
