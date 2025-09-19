using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.CSharp.Generators.TypeMapperGenerator;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.TypeNames;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class ResultFromEntityTypeMapperGenerator : ClassBaseGenerator<ResultFromEntityDescriptor>
{
    private const string Entity = "entity";
    private const string EntityStore = "entityStore";
    private const string Map = "Map";
    private const string Snapshot = "snapshot";

    protected override bool CanHandle(
        ResultFromEntityDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings)
        => !settings.NoStore && base.CanHandle(descriptor, settings);

    protected override void Generate(
        ResultFromEntityDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        fileName = descriptor.ExtractMapperName();
        path = State;
        ns = CreateStateNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal);
        var entityType = CreateEntityType(
            descriptor.Name,
            descriptor.RuntimeType.NamespaceWithoutGlobal);

        var classBuilder = ClassBuilder
            .New()
            .SetAccessModifier(settings.AccessModifier)
            .AddImplements(
                IEntityMapper.WithGeneric(
                    descriptor.ExtractType().ToString(),
                    descriptor.RuntimeType.Name))
            .SetName(fileName);

        var constructorBuilder = ConstructorBuilder
            .New()
            .SetTypeName(descriptor.Name);

        AddConstructorAssignedField(
            IEntityStore,
            GetFieldName(EntityStore),
            EntityStore,
            classBuilder,
            constructorBuilder);

        // Define map method
        var mapMethod = MethodBuilder
            .New()
            .SetName(Map)
            .SetAccessModifier(AccessModifier.Public)
            .SetReturnType(descriptor.RuntimeType.Name)
            .AddParameter(
                ParameterBuilder
                    .New()
                    .SetType(
                        descriptor.Kind is TypeKind.Entity
                            ? entityType.FullName
                            : descriptor.Name)
                    .SetName(Entity))
            .AddParameter(
                Snapshot,
                b => b.SetDefault("null")
                    .SetType(IEntityStoreSnapshot.MakeNullable()));

        mapMethod
            .AddCode(IfBuilder
                .New()
                .SetCondition($"{Snapshot} is null")
                .AddCode(AssignmentBuilder
                    .New()
                    .SetLeftHandSide(Snapshot)
                    .SetRightHandSide($"{GetFieldName(EntityStore)}.CurrentSnapshot")))
            .AddEmptyLine();

        // creates the instance of the model that is being mapped.
        var createModelCall =
            MethodCallBuilder
                .New()
                .SetReturn()
                .SetNew()
                .SetMethodName(descriptor.RuntimeType.Name);

        foreach (var property in descriptor.Properties)
        {
            createModelCall.AddArgument(BuildMapMethodCall(settings, Entity, property));
        }

        mapMethod.AddCode(createModelCall);
        classBuilder.AddConstructor(constructorBuilder);

        classBuilder.AddMethod(mapMethod);

        var processed = new HashSet<string>();

        AddRequiredMapMethods(
            settings,
            descriptor,
            classBuilder,
            constructorBuilder,
            processed);

        foreach (var deferred in descriptor.Deferred)
        {
            createModelCall.AddArgument(
                MethodCallBuilder
                    .Inline()
                    .SetMethodName(Map + deferred.Class.RuntimeType.Name)
                    .AddArgument(Entity)
                    .AddArgument(Snapshot));

            AddMapFragmentMethod(
                classBuilder,
                constructorBuilder,
                deferred.Class,
                deferred.InterfaceName,
                entityType.FullName,
                processed);
        }

        classBuilder.Build(writer);
    }
}
