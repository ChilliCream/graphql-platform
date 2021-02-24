using System;
using System.Linq;
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
            ComplexTypeDescriptor complexTypeDescriptor =
                typeDescriptor as ComplexTypeDescriptor ??
                throw new InvalidOperationException(
                    "A result entity mapper can only be generated for complex types");

            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            var className = CreateResultInfoName(complexTypeDescriptor.RuntimeType.Name);
            fileName = className;

            classBuilder
                .AddImplements(TypeNames.IOperationResultDataInfo)
                .SetName(fileName);

            constructorBuilder
                .SetTypeName(complexTypeDescriptor.RuntimeType.Name)
                .SetAccessModifier(AccessModifier.Public);


            foreach (var prop in complexTypeDescriptor.Properties)
            {
                var propTypeBuilder = prop.Type.ToEntityIdBuilder();

                // Add Property to class
                classBuilder.AddProperty(
                    prop.Name,
                    x => x.SetType(propTypeBuilder).SetAccessModifier(AccessModifier.Public));

                // Add initialization of property to the constructor
                var paramName = prop.Name.WithLowerFirstChar();
                ParameterBuilder parameterBuilder = ParameterBuilder.New()
                    .SetName(paramName)
                    .SetType(propTypeBuilder);

                constructorBuilder.AddParameter(parameterBuilder);
                constructorBuilder.AddCode(prop.Name + " = " + paramName + ";");
            }

            classBuilder.AddProperty(
                "EntityIds",
                x => x.SetType(TypeNames.IReadOnlyCollection.WithGeneric(TypeNames.EntityId))
                    .AsLambda("_entityIds"));

            classBuilder.AddProperty("Version", x => x.SetType("ulong").AsLambda("_version"));

            AddConstructorAssignedField(
                $"{TypeNames.IReadOnlyCollection}<{TypeNames.EntityId}>",
                "_entityIds",
                classBuilder,
                constructorBuilder);

            AddConstructorAssignedField(
                "ulong",
                "_version",
                classBuilder,
                constructorBuilder,
                true);


            // WithVersion

            classBuilder.AddMethod(
                "WithVersion",
                x => x.SetAccessModifier(AccessModifier.Public)
                    .SetReturnType(TypeNames.IOperationResultDataInfo)
                    .AddParameter(ParameterBuilder.New()
                        .SetType("ulong")
                        .SetName("version"))
                    .AddCode(MethodCallBuilder.New()
                        .SetPrefix("return new ")
                        .SetMethodName(className)
                        .AddArgumentRange(
                            complexTypeDescriptor.Properties.Select(x => x.Name.Value))
                        .AddArgument("_entityIds")
                        .AddArgument("_version")));


            CodeFileBuilder
                .New()
                .SetNamespace(complexTypeDescriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
