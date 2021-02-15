using System;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultInfoGenerator : ClassBaseGenerator<ITypeDescriptor>
    {
        protected override bool CanHandle(ITypeDescriptor descriptor)
        {
            return descriptor.Kind == TypeKind.ResultType && !descriptor.IsInterface();
        }

        protected override void Generate(
            CodeWriter writer,
            ITypeDescriptor typeDescriptor,
            out string fileName)
        {
            NamedTypeDescriptor namedTypeDescriptor = typeDescriptor switch
            {
                NamedTypeDescriptor nullableNamedType => nullableNamedType,
                NonNullTypeDescriptor {InnerType: NamedTypeDescriptor namedType} => namedType,
                _ => throw new ArgumentException(nameof(typeDescriptor))
            };

            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            fileName = ResultInfoNameFromTypeName(namedTypeDescriptor.Name);

            classBuilder
                .AddImplements(TypeNames.IOperationResultDataInfo)
                .SetName(fileName);

            constructorBuilder
                .SetTypeName(namedTypeDescriptor.Name)
                .SetAccessModifier(AccessModifier.Public);

            var constructorCaller = MethodCallBuilder.New()
                .SetPrefix("return new ")
                .SetMethodName(fileName);

            var withVersion = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(TypeNames.IOperationResultDataInfo)
                .SetName($"WithVersion")
                .AddParameter(ParameterBuilder.New()
                    .SetType("ulong")
                    .SetName("version"));

            foreach (var prop in namedTypeDescriptor.Properties)
            {
                var propTypeBuilder = prop.Type.ToEntityIdBuilder();

                // Add Property to class
                var propBuilder = PropertyBuilder
                    .New()
                    .SetName(prop.Name)
                    .SetType(propTypeBuilder)
                    .SetAccessModifier(AccessModifier.Public);

                classBuilder.AddProperty(propBuilder);
                constructorCaller.AddArgument(prop.Name);

                // Add initialization of property to the constructor
                var paramName = prop.Name.WithLowerFirstChar();
                ParameterBuilder parameterBuilder = ParameterBuilder.New()
                    .SetName(paramName)
                    .SetType(propTypeBuilder);

                constructorBuilder.AddParameter(parameterBuilder);
                constructorBuilder.AddCode(prop.Name + " = " + paramName + ";");
            }

            classBuilder.AddProperty(PropertyBuilder.New()
                .SetName($"EntityIds")
                .SetType($"{TypeNames.IReadOnlyCollection}<{TypeNames.EntityId}>")
                .AsLambda("_entityIds"));

            classBuilder.AddProperty(PropertyBuilder.New()
                .SetName("Version")
                .SetType("ulong")
                .AsLambda("_version"));

            AddConstructorAssignedField(
                $"{TypeNames.IReadOnlyCollection}<{TypeNames.EntityId}>",
                "_entityIds",
                classBuilder,
                constructorBuilder);
            constructorCaller.AddArgument("_entityIds");

            AddConstructorAssignedField(
                "ulong",
                "_version",
                classBuilder,
                constructorBuilder,
                true);
            constructorCaller.AddArgument("_version");

            withVersion.AddCode(constructorCaller);
            classBuilder.AddMethod(withVersion);

            CodeFileBuilder
                .New()
                .SetNamespace(namedTypeDescriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
