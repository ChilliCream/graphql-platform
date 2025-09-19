using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class ResultInfoGenerator : ClassBaseGenerator<ITypeDescriptor>
{
    private const string UnderscoreEntityIds = "_entityIds";
    private const string EntityIds = "entityIds";
    private const string UnderscoreVersion = "_version";
    private const string Version = "version";

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
        var complexTypeDescriptor =
            typeDescriptor as ComplexTypeDescriptor ??
            throw new InvalidOperationException(
                "A result entity mapper can only be generated for complex types");

        var className = CreateResultInfoName(complexTypeDescriptor.RuntimeType.Name);
        fileName = className;
        path = State;
        ns = CreateStateNamespace(complexTypeDescriptor.RuntimeType.NamespaceWithoutGlobal);

        var classBuilder = ClassBuilder
            .New()
            .SetAccessModifier(settings.AccessModifier)
            .AddImplements(TypeNames.IOperationResultDataInfo)
            .SetName(fileName);

        var constructorBuilder = classBuilder
            .AddConstructor()
            .SetTypeName(complexTypeDescriptor.RuntimeType.Name);

        foreach (var prop in complexTypeDescriptor.Properties)
        {
            var propTypeBuilder = prop.Type.ToStateTypeReference();

            // Add Property to class
            classBuilder
                .AddProperty(prop.Name)
                .SetComment(prop.Description)
                .SetType(propTypeBuilder)
                .SetPublic();

            // Add initialization of property to the constructor
            var paramName = GetParameterName(prop.Name);
            constructorBuilder.AddParameter(paramName).SetType(propTypeBuilder);
            constructorBuilder.AddCode(
                AssignmentBuilder
                    .New()
                    .SetLeftHandSide(GetLeftPropertyAssignment(prop.Name))
                    .SetRightHandSide(paramName));
        }

        classBuilder
            .AddProperty("EntityIds")
            .SetType(TypeNames.IReadOnlyCollection.WithGeneric(TypeNames.EntityId))
            .AsLambda(settings.IsStoreEnabled()
                ? CodeInlineBuilder.From(UnderscoreEntityIds)
                : MethodCallBuilder.Inline()
                    .SetMethodName(TypeNames.Array, "Empty")
                    .AddGeneric(TypeNames.EntityId));

        classBuilder
            .AddProperty("Version")
            .SetType(TypeNames.UInt64)
            .AsLambda(settings.IsStoreEnabled() ? UnderscoreVersion : "0");

        if (settings.IsStoreEnabled())
        {
            AddConstructorAssignedField(
                TypeNames.IReadOnlyCollection.WithGeneric(TypeNames.EntityId),
                UnderscoreEntityIds,
                EntityIds,
                classBuilder,
                constructorBuilder);

            AddConstructorAssignedField(
                TypeNames.UInt64,
                UnderscoreVersion,
                Version,
                classBuilder,
                constructorBuilder,
                true);
        }

        // WithVersion
        classBuilder
            .AddMethod("WithVersion")
            .SetAccessModifier(AccessModifier.Public)
            .SetReturnType(TypeNames.IOperationResultDataInfo)
            .AddParameter(Version, x => x.SetType(TypeNames.UInt64))
            .AddCode(MethodCallBuilder
                .New()
                .SetReturn()
                .SetNew()
                .SetMethodName(className)
                .AddArgumentRange(
                    complexTypeDescriptor.Properties.Select(x => x.Name))
                .If(settings.IsStoreEnabled(),
                    x => x.AddArgument(UnderscoreEntityIds).AddArgument(Version)));

        classBuilder.Build(writer);
    }
}
