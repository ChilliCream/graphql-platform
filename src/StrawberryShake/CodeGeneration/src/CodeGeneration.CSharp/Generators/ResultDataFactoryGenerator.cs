using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class ResultDataFactoryGenerator : TypeMapperGenerator
{
    private const string entityStore = "entityStore";
    private const string _entityStore = "_entityStore";
    private const string _dataInfo = "dataInfo";
    private const string _snapshot = "snapshot";
    private const string _info = "info";

    protected override bool CanHandle(ITypeDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings)
    {
        return descriptor.Kind == TypeKind.Result && !descriptor.IsInterface();
    }

    protected override void Generate(ITypeDescriptor typeDescriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        var descriptor =
            typeDescriptor as ComplexTypeDescriptor ??
            throw new InvalidOperationException(
                "A result data factory can only be generated for complex types");

        fileName = CreateResultFactoryName(descriptor.RuntimeType.Name);
        path = State;
        ns = CreateStateNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal);

        var classBuilder =
            ClassBuilder
                .New()
                .SetAccessModifier(settings.AccessModifier)
                .SetName(fileName)
                .AddImplements(
                    TypeNames.IOperationResultDataFactory.WithGeneric(descriptor.RuntimeType));

        var constructorBuilder = classBuilder
            .AddConstructor()
            .SetTypeName(descriptor.Name);

        if (settings.IsStoreEnabled())
        {
            AddConstructorAssignedField(
                TypeNames.IEntityStore,
                _entityStore,
                entityStore,
                classBuilder,
                constructorBuilder);
        }

        var returnStatement = MethodCallBuilder
            .New()
            .SetReturn()
            .SetNew()
            .SetMethodName(descriptor.RuntimeType.Name);

        foreach (var property in descriptor.Properties)
        {
            returnStatement
                .AddArgument(BuildMapMethodCall(settings, _info, property));
        }

        var ifHasCorrectType = IfBuilder
            .New()
            .SetCondition(
                $"{_dataInfo} is {CreateResultInfoName(descriptor.RuntimeType.Name)} {_info}")
            .AddCode(returnStatement);

        var createMethod = classBuilder
            .AddMethod("Create")
            .SetAccessModifier(AccessModifier.Public)
            .SetReturnType(descriptor.RuntimeType.Name)
            .AddParameter(_dataInfo, b => b.SetType(TypeNames.IOperationResultDataInfo))
            .AddParameter(
                _snapshot,
                b => b.SetDefault("null")
                    .SetType(TypeNames.IEntityStoreSnapshot.MakeNullable()));

        if (settings.IsStoreEnabled())
        {
            createMethod
                .AddCode(
                    IfBuilder.New()
                        .SetCondition($"{_snapshot} is null")
                        .AddCode(
                            AssignmentBuilder
                                .New()
                                .SetLeftHandSide(_snapshot)
                                .SetRightHandSide($"{_entityStore}.CurrentSnapshot")))
                .AddEmptyLine();
        }

        createMethod.AddCode(ifHasCorrectType)
            .AddEmptyLine()
            .AddCode(
                ExceptionBuilder
                    .New(TypeNames.ArgumentException)
                    .AddArgument(
                        $"\"{CreateResultInfoName(descriptor.RuntimeType.Name)} expected.\""));

        var processed = new HashSet<string>();

        AddRequiredMapMethods(
            settings,
            descriptor,
            classBuilder,
            constructorBuilder,
            processed,
            true);

        classBuilder
            .AddProperty("ResultType")
            .SetType(TypeNames.Type)
            .AsLambda($"typeof({descriptor.RuntimeType.Namespace}.{descriptor.Implements[0]})")
            .SetInterface(TypeNames.IOperationResultDataFactory);

        classBuilder
            .AddMethod("Create")
            .SetInterface(TypeNames.IOperationResultDataFactory)
            .SetReturnType(TypeNames.Object)
            .AddParameter(_dataInfo, b => b.SetType(TypeNames.IOperationResultDataInfo))
            .AddParameter(
                _snapshot,
                b => b.SetType(TypeNames.IEntityStoreSnapshot.MakeNullable()))
            .AddCode(
                MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetMethodName("Create")
                    .AddArgument(_dataInfo)
                    .AddArgument(_snapshot));

        classBuilder.Build(writer);
    }
}
